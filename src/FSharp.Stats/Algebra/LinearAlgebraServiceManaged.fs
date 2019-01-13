// (c) Microsoft Corporation 2005-2009.

//namespace Microsoft.FSharp.Math.LinearAlgebra // old namespace
namespace FSharp.Stats.Algebra

//open Microsoft.FSharp.Math
//open Microsoft.FSharp.Math. Bindings.Internals.NativeUtilities
open FSharp.Stats

/// This module is for internal use only.
module LinearAlgebraManaged = 

    //let a = 1.    

    let private matrixDims (m:Matrix<_>) = m.NumRows, m.NumCols

    let NYI () = failwith "Not yet implemented, managed fallback linear algebra ops coming soon"
    
    //type Permutation = Permutation of int * (int -> int)
    
    let SVD (a:matrix) =
        let (umatrix,s,vmatrix) = SVD.computeInPlace (a.ToArray2D())
        //Matrix.diag
        Vector.ofArray s,Matrix.ofArray2D umatrix,Matrix.ofArray2D vmatrix
        //(Matrix.ofArray2D umatrix,s,Matrix.ofArray2D vmatrix)
    
    let symmetricEigenspectrum (a:matrix) = 
        EVD.symmetricEvd (a.ToArray2D())
        |> EVD.getRealEigenvalues


    let EigenSpectrum A = NYI()
    let Condition A = NYI()

    let eigenvectors m = NYI()
    let eigenvalues m = NYI()
    let symmetricEigenvalues a = NYI()
    let symmetricEigenvectors a = NYI()

    let SolveTriangularLinearSystems K B isLower =
        if isLower then
            let (nK,mK) = matrixDims K
            let (nB,mB) = matrixDims B
            if nK<>mK || nB<> nK then invalidArg "Matrix" "Cannot do backward substitution on non-square matrices."
            let X = Matrix.zero(max nB nK) (max mB mK) // t
            for i=0 to nK-1 do
                for k=0 to mB-1 do
                    let s = ref B.[i,k]
                    for j=0 to i-1 do
                        s := !s - K.[i,j] * X.[j,k]
                    done
                    X.[i,k] <- !s / K.[i,i]
                done
            done
            X
        else
            let (nK,mK) = matrixDims K
            let (nB,mB) = matrixDims B
            if nK<>mK || nB<> nK then invalidArg "Matrix" "Cannot do backward substitution on non-square matrices."
            let X = Matrix.zero (max nB nK) (max mB mK) //t
            for i=0 to nK-1 do
                for k=0 to mB-1 do
                    let s = ref B.[nK-i-1,k]
                    for j=0 to i-1 do
                        s := !s - K.[nK-i-1,nK-j-1] * X.[nK-j-1,k]
                    done
                    X.[nK-i-1,k] <- !s / K.[nK-i-1,nK-i-1]
                done
            done
            X

    let SolveTriangularLinearSystem K v isLower = Matrix.getCol (SolveTriangularLinearSystems K (Matrix.ofVector v) isLower) 0

    type range = int * int

    let inline sumfR f ((a,b):range) =
        let mutable res = 0.0 in
        for i = a to b do
            res <- res + f i
        res
      

    let Cholesky (a: matrix) =
        let nA,mA = a.Dimensions
        if nA<>mA              then invalidArg "Matrix" "choleskyFactor: not square";
        let lres = Matrix.zero nA nA (* nA=mA *)
        for j=0 to nA-1 do
        for i=j to nA-1 do (* j <= i *)
          (* Consider a_ij = sum(k=0...n-1)  (lres_ik . lresT_kj)
           *               = sum(k=0...n-1)  (lres_ik . lres_jk)
           *               = sum(k=0...j-1)  (lres_ik . lres_jk) + lres_ij . lres_jj + (0 when k>j)
           *               = psum                                + lres_ij . lres_jj
           * This determines lres_ij terms of preceeding terms.
           * lres_ij depends on lres_ik and lres_jk (and maybe lres_ii) for k<i
           *)
          let psum = sumfR (fun k -> lres.[i,k] * lres.[j,k]) (0,j-1)
          let a_ij = a.[i,j]
          if i=j then
            let t = (a_ij - psum)
            if t >= 0.0 then lres.[i,i] <- (System.Math.Sqrt t) else invalidArg "Matrix" "choleskyFactor: not symmetric postive definite"
          else
            lres.[i,j] <- ((a_ij - psum) / lres.[j,j])
        done
        done;
        // if not (isLowerTriangular lres) then failwith "choleskyFactor: not lower triangular result";
        lres.Transpose  // REVIEW optimize this so we don't have to take transpose ...
    

    /// For a matrix A, the LU factorization is a pair of lower triangular matrix L and upper triangular matrix U so that A = L*U.
    /// The pivot function encode a permutation operation such for a matrix P P*A = L*U.
    let LU A =
        let (nA,mA) = matrixDims A
        if nA<>mA then invalidArg "Matrix" "lu: not square"
        let L = Matrix.zero nA nA
        let U = Matrix.copy A
        let P = [| 0 .. nA-1 |]
        let abs (x:float) = System.Math.Abs x
        let swapR X i j =                           //  REVIEW should we make this a general method?
            let (nX,mX) = matrixDims X
            let t = X.[i .. i,0 .. ]
            for k=0 to mX-1 do
                X.[i,k] <- X.[j,k]
                X.[j,k] <- t.[0,k]
            done
            
        for i=0 to nA-2 do
            let mutable maxi = i        //  Find the biggest pivot element.
            for k=i+1 to nA-1 do
                if abs(U.[maxi,i]) < abs(U.[k,i]) then maxi <- k
            done
            //let maxi = maxIndex (i+1) (nA-1) (fun k -> abs(U.[k,i]))
            
            if maxi <> i then
                swapR U i maxi     // Swap rows if needed.
                swapR L i maxi     // REVIEW can be made more performant.
                let t = P.[i]
                P.[i] <- P.[maxi]
                P.[maxi] <- t
            
            for j=i+1 to nA-1 do
                L.[j,i] <- U.[j,i] / U.[i,i]
                for k=i+1 to nA-1 do
                    U.[j,k] <- U.[j,k] - L.[j,i] * U.[i,k]
                done
                U.[j,i] <- 0.0
            done
        done
        (((*P.Length,*)Permutation.ofArray P),L + Matrix.identity nA,U)
        //(P, L + (Matrix.identity nA), U)
    

    /// Solves a system of linear equations, AX = B, with A LU factorized.
    let SolveLinearSystem (A:matrix) (b:vector) =
        let (n,m) = matrixDims A
        if n <> m then invalidArg "Matrix" "Matrix must be square." 
        let P,L,U = LU A
        (SolveTriangularLinearSystem U (SolveTriangularLinearSystem L (b.Permute P) true) false)

    /// Solves a system of linear equations, Ax = b, with A LU factorized.        
    let SolveLinearSystems (A:matrix) (B:matrix) =
        let (n,m) = matrixDims A
        if n <> m then invalidArg "Matrix" "Matrix must be square." 
        let P,L,U = LU A
        (SolveTriangularLinearSystems U (SolveTriangularLinearSystems L (B.PermuteRows P) true) false)
    
    let Inverse A =
        let (n,m) = matrixDims A
        if n <> m then invalidArg "Matrix" "Matrix must be square when computing its inverse." 
        let P,L,U = LU A
        (SolveTriangularLinearSystems U (SolveTriangularLinearSystems L ((Matrix.identity n).PermuteRows P) true) false)
        
    /// Generates a unit vector [1 0 .. 0 ].
    let unitV k = let e = Vector.create k 0.0 in e.[0] <- 1.0; e

    /// Computes the sign of a floating point number.
    let sign (f: float) = float (System.Math.Sign f)                    // REVIEW put in float library.

    /// This method computes and performs a Householder reflection. It will change the
    /// input matrix and return the reflection vector.
    let HouseholderTransform (A:matrix) (i:int) =
        // REVIEW do this using views and get rid of the i.
        let (n,m) = matrixDims A
        //Old: let x = A.[i..,i..i].Column 0                       // Get part of the i'th column of the matrix.
        let x = Matrix.getCol A.[i..,i..i] 0                       // Get part of the i'th column of the matrix.         
        let nx = Vector.norm x
        let vu = x + sign(x.[0]) * nx * (unitV (n-i))               // Compute the reflector.
        let v = 1.0/(Vector.norm vu) * vu                           // Normalize reflector.
        
        // Perform the reflection.
        let v'A = RowVector.init (m-i) (fun j -> v.Transpose * (A.[i..,i+j..i+j].Column 0))
        for l=i to n-1 do
            for k=i to m-1 do
                A.[l,k] <- A.[l,k] - 2.0 * v.[l-i] * v'A.[k-i]
        v                                                              // Return reflection vector.
            
    let QR (A:matrix) =
        let (n,m) = matrixDims A
        let mutable Q = Matrix.identity n                                   // Keeps track of the orthogonal matrix.
        let R = Matrix.copy A

        // This method will update the orhogonal transformation fast when given a reflection vector.
        let UpdateQ (qm:matrix) (v:vector) =
            let n = Vector.length v
            let (nQ,mQ) = matrixDims qm
            
            // Cache the computation of Q*v.
            let Qv = Vector.init nQ (fun i -> (qm.[i..i,nQ-n..].Row 0) * v)

            // Update the orthogonal transformation.
            for i=0 to nQ-1 do
                for j=nQ-n to nQ-1 do
                    qm.[i,j] <- qm.[i,j] - 2.0 * Qv.[i] * v.[j-nQ+n]
            ()
        
        // This QR implementation keeps the unreduced part of A in R. It computes reflectors one at a time
        // and reduces R column by column. In the process it keeps track of the Q matrix.
        for i=0 to (min n m)-1 do
            let v = HouseholderTransform R i
            UpdateQ Q v
        Q,R

    let Hessenberg (A:matrix) =
        // REVIEW we can do with less copying here.
        let (n,m) = matrixDims A
        if n<>m then invalidArg "Matrix A" "Currently only implemented for square matrices."
        let mutable Q = Matrix.identity n                                   // Keeps track of the orthogonal matrix.
        let R = A.[1..,*]

        // This method will update the orhogonal transformation fast when given a reflection vector.
        let UpdateQ (qm:Matrix<float>) (v:vector) =
            let n = Vector.length v
            let (nQ,mQ) = matrixDims qm
            
            // Cache the computation of Q*v.
            let Qv = Vector.init nQ (fun i -> (qm.[i..i,nQ-n..].Row 0) * v)

            // Update the orthogonal transformation.
            for i=0 to nQ-1 do
                for j=nQ-n to nQ-1 do
                    qm.[i,j] <- qm.[i,j] - 2.0 * Qv.[i] * v.[j-nQ+n]
            ()
        
        // This QR implementation keeps the unreduced part of A in R. It computes reflectors one at a time
        // and reduces R column by column. In the process it keeps track of the Q matrix.
        for i=0 to n-2 do
            let v = HouseholderTransform R i
            UpdateQ Q v
        Q,Matrix.init n m (fun i j -> if i = 0 then A.[i,j] else R.[i-1,j])
        
    let leastSquares A (b: vector) =
        let (m,n) = matrixDims A
        // Is this an overdetermined or underdetermined system?
        if m > n then
            let qm,R = QR A
            let Qtb = qm.Transpose * b
            SolveTriangularLinearSystem R.[0..n-1,0..n-1] Qtb.[0..n-1] false
        else
            let qm,R = QR A
            let Qtb = qm.Transpose * b
            let s = SolveTriangularLinearSystem R.[0..m-1,0..m-1] Qtb false
            Vector.init n (fun i -> if i < m then s.[i] else 0.0)

    /// computes the hat matrix by the QR decomposition of the designmatrix used in ordinary least squares approaches
    let hatMatrix (designMatrix: Matrix<float>) = 
        let qm,R = QR designMatrix
        let q1 = qm.GetSlice ((Some 0),(Some (qm.NumRows-1)),(Some 0),(Some (R.NumCols-1)))
        // computes the hatmatrix 
        q1 * q1.Transpose

    
    /// computes the leverages of every dataPoint of a dataSet given by the diagonal of the hat matrix. 
    let leverageBy (hatMatrix: Matrix<float>) = 
        hatMatrix.Diagonal

    /// computes the leverage directly by QR decomposition of the designmatrix used in ordinary least squares approaches
    /// and computing of the diagnonal entries of the Hat matrix, known as the leverages of the regressors
    let leverage (designMatrix: Matrix<float>) = 
        let qm,R = QR designMatrix
        let q1 = qm.GetSlice ((Some 0),(Some (qm.NumRows-1)),(Some 0),(Some (R.NumCols-1)))
        let leverage = 
            let diagonal = FSharp.Stats.Vector.create q1.NumRows 0. 
            diagonal 
            |> FSharp.Stats.Vector.mapi (fun i x -> 
                                            FSharp.Stats.Matrix.foldRow (fun acc x -> acc + (x ** 2.)) 0. q1 i 
                                        )
        leverage
        
    
    /// Calculates the pseudo inverse of the matrix
    let pseudoInvers (matrix:Matrix<float>) =
        let (m,n) = matrixDims matrix
        // Is this an overdetermined or underdetermined system?
        if m > n then
            let qm,R = QR matrix
            let i = Matrix.identity m
            let Qtb = qm.Transpose * i
            SolveTriangularLinearSystems R.[0..n-1,0..n-1] Qtb.[0..n-1,0..m-1] false
        else
            let qm,R = QR matrix.Transpose
            let i = Matrix.identity n
            let Qtb = qm.Transpose * i        
            let s = SolveTriangularLinearSystems R.[0..m-1,0..m-1] Qtb.[0..m-1,0..n-1] false
            s.Transpose            



