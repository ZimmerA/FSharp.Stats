(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/FSharp.Stats/net461"
#r "../../packages/build/FSharp.Plotly/lib/net45/Fsharp.Plotly.dll"
open FSharp.Plotly
(**
#Statistical testing


FSharp.Stats a provides 
A hypothesis test is a statistical test that is used to determine whether there is enough evidence 
in a sample of data to infer that a certain condition is true for the entire population. 
A hypothesis test examines two opposing hypotheses about a population: the null hypothesis and the alternative hypothesis.

<a name="TestStatistics"></a>

##Test Statistics

<a name="Anova"></a>

##Anova

<a name="TTest"></a>

##T-Test

<a name="ChiSquareTest"></a>

##Chi-Squared Test

<a name="Bartlett"></a>

##Bartlett

<a name="PostHoc"></a>

##PostHoc

<a name="PvalueAdjust"></a>

##Adjusting P-Values

*)
#r "FSharp.Stats.dll"
#r "netstandard.dll"
open FSharp.Stats
open FSharp.Stats.Testing


let sample1 = [|-0.2268419965; -0.3772357485|] |> FSharp.Stats.Vector.ofArray
let sample2 = [|-0.6076633409; -0.1781469665|] |> FSharp.Stats.Vector.ofArray

Testing.TTest.twoSample false sample1  sample2

// Example from:  http://www.statstutor.ac.uk/resources/uploaded/paired-t-test.pdf
// A paired t-test is used to compare two population means where you have two samples in
// which observations in one sample can be paired with observations in the other sample.
// Examples of where this might occur are:
//      -   Before-and-after observations on the same subjects (e.g. students’ diagnostic test
//          results before and after a particular module or course).
//      -   A comparison of two different methods of measurement or two different treatments
//          where the measurements/treatments are applied to the same subjects (e.g. blood
//          pressure measurements using a stethoscope and a dynamap).

let sampleP1 = vector [18.;21.;16.;22.;19.;24.;17.;21.;23.;18.;14.;16.;16.;19.;18.;20.;12.;22.;15.;17.;]
let sampleP2 = vector [22.;25.;17.;24.;16.;29.;20.;23.;19.;20.;15.;15.;18.;26.;18.;24.;18.;25.;19.;16.;]

Testing.TTest.twoSamplePaired sampleP1 sampleP2

// http://astatsa.com/OneWay_Anova_with_TukeyHSD/
let dataOneWay =
    [|
    [|0.28551035; 0.338524035; 0.088313218; 0.205930807; 0.363240102;|];
    [|0.52173913; 0.763358779; 0.32546786; 0.425305688; 0.378071834; |];
    [|0.989119683; 1.192718142; 0.788288288; 0.549176236; 0.544588155;|];
    [|1.26705653; 1.625320787; 1.266108976; 1.154187629; 1.268498943; 1.069518717;|];
    |]



let contrastMatrix = 
    [| [|1.0;-1.0;0.0;0.0;|] ; [|1.0;0.0;-1.0;0.0;|] ; [|1.0;0.;0.0;-1.0;|] ; 
    [|0.0;1.0;-1.0;0.0;|]    ; [|0.0;1.0;0.0;-1.0;|] ;
    [|0.0;0.0;1.0;-1.0;|]
    |]

Anova.oneWayAnova dataOneWay

//Testing.Hays contrastMatrix 

Testing.PostHoc.Hays contrastMatrix dataOneWay 



open TestStatistics
        
type Contrast = { Index            : int;                      
                    L                : float;
                    DegreesOfFreedom : float;
                    MeanSquares      : float;
                    Significance     : float;                      
                    Statistic        : float;
                    SumOfSquares     : float;
                    }
    
let createContrast index l degreesOfFreedom meanSquares significance statistic sumOfSquares =
    {Index = index; L = l; DegreesOfFreedom = degreesOfFreedom; MeanSquares = meanSquares; Significance = significance; Statistic = statistic; SumOfSquares = sumOfSquares;}


//let sumOfSquareContrast (sampleSizes:int[]) (sampleMeans:float[]) (contrast:float[]) =        
//    let l           =  Array.fold2 (fun state mi ai -> state + (mi * ai)) 0.0 sampleMeans contrast 
//    let denominator = (Array.map2 (fun a n -> (abs a) / (float n)) contrast sampleSizes) |> Array.sum
//    (l * l / denominator,l)


let Scheffe (contrastMatrix:float[][]) (data:float[][]) =

    let calcStats (sampleSizes:int[]) (sampleMeans:float[]) (contrast:float[]) =        
        let l           =  Array.fold2 (fun state mi ai -> state + (mi * ai)) 0.0 sampleMeans contrast 
        let denominator = (Array.map2 (fun c n -> (abs c) / (float n)) contrast sampleSizes) |> Array.sum
        (l, denominator)
        
    // Sample sizes
    let sizes = data |> Array.map (fun x -> x.Length)
    let totalSize = sizes |> Array.sum
    let groupCount = data.Length
    // Degrees of freedom
    let Db = float(groupCount - 1)
    let Dw = float(totalSize - groupCount)
    let Dt = groupCount * totalSize - 1

    // Step 1. Calculate the mean within each group
    let sampleMeans = data |> Array.map Seq.mean        
    // Step 2. Calculate the sum of squares contrast associated
    let stats = contrastMatrix |> Array.map (fun ar -> calcStats sizes sampleMeans ar)
    // Step 3. Calculate the weighted "within-group" sum of squares
    let Sw = 
        data
        |> Array.mapi (fun i ar -> 
            let tmp =
                ar 
                |> Array.fold (fun acc elem -> 
                    acc + ((elem-sampleMeans.[i])**2.0)) 0.0
            float (sizes.[i] - 1) * tmp) |> Array.sum
    let MSw = Sw / Dw // within-group mean square or MSerror
    
    // Step 5. Calculate the F statistic per contrast
    Array.mapi  (fun i (l, denominator) ->
        let fValue = ((l * l) / Db) / (MSw * denominator)
        printfn "%f" ((l * l) / Db)
        if nan.Equals(fValue) then
            createContrast i l Db MSw nan nan Sw
        else
            let FTest = createFTest fValue Db Dw
            createContrast i l Db MSw FTest.PValue fValue Sw   
                ) stats


Scheffe contrastMatrix dataOneWay 


//let m1 = Seq.mean dataOneWay.[0]
//let m2 = Seq.mean dataOneWay.[1]

//let s1 = Seq.sum dataOneWay.[0]
//let s2 = Seq.sum dataOneWay.[1]
//let s3 = Seq.sum dataOneWay.[2]


//let d = (m1-m2)**2.0
//d / (3.926003843 * (1./5. + 1./5.))

// http://www.statisticslectures.com/topics/posthoconewayanova/
let dmg = 
    [|
     [|9.;8.;7.;8.;8.;9.;8.;|];
     [|7.;6.;6.;7.;8.;7.;6.;|];
     [|4.;3.;2.;3.;4.;3.;2.;|] ; 
    |]

let contrastMatrixDmg = 
    [| 
     [|1.0;-1.0;0.0;|] ; 
     [|1.0;0.0;-1.0;|] ; 
     [|0.0;1.0;-1.0;|]   
    |]


Scheffe contrastMatrixDmg dmg 

// Tuky HSD



dmg.[2] |> Seq.sum


Anova.oneWayAnova dmg

/// Tukey-Kramer approach
let TukeyHSD (contrastMatrix:float[][]) (data:float[][]) =

    let calcStats (msw:float) (sampleSizes:int[]) (sampleMeans:float[]) (contrast:float[]) =        
        let l           =  Array.fold2 (fun state mi ai -> state + (mi * ai)) 0.0 sampleMeans contrast 
        let denominator = (Array.map2 (fun a n -> (abs a) * (msw / (float n))) contrast sampleSizes) |> Array.sum
        //printfn "msw: %f d: %f" msw (denominator)
        ((l / (sqrt (denominator))),l)
        
    // Sample sizes
    let sizes = data |> Array.map (fun x -> x.Length)
    let totalSize = sizes |> Array.sum
    let groupCount = data.Length
    // Degrees of freedom
    let Db = float(groupCount - 1)
    let Dw = float(totalSize - groupCount)
    let Dt = groupCount * totalSize - 1

    // Step 1. Calculate the mean within each group
    let sampleMeans = data |> Array.map Seq.mean        

    // Step 2. Calculate the "within-group" sum of squares
    let Sw = data|> Array.mapi (fun i ar -> ar |> Array.fold (fun acc elem -> acc + ((elem-sampleMeans.[i])**2.0)) 0.0) |> Array.sum
    let MSw = Sw / Dw // within-group mean square or MSerror
    
    // Step 3. 
    let stats = contrastMatrix |> Array.map (fun ar -> calcStats MSw sizes sampleMeans ar)
    
    // Step 4. Calculate the F statistic per contrast
    Array.mapi  (fun i (tValue,l)  ->
                        if nan.Equals(tValue) then
                            createContrast i l Db MSw nan nan Sw  
                        else
                            let TTest = createTTest tValue Dw
                            createContrast i l Db MSw TTest.PValue TTest.Statistic Sw
                                      
                ) stats
    



//TukeyHSD contrastMatrixDmg dmg 
// https://brownmath.com/stat/anova1.htm

let dsd = 
    [|
     //[|7.;4.;6.;8.;6.;6.;2.;9.|];
     //[|5.;5.;3.;4.;4.;7.;2.;2.|];
     //[|2.;4.;7.;1.;2.;1.;5.;5.|] ; 
        [|64.; 72.; 68.; 77.; 56.; 95.;|] ;
        [|78.; 91.; 97.; 82.; 85.; 77.;|] ;
        [|75.; 93.; 78.; 71.; 63.; 76.;|] ;
        [|55.; 66.; 49.; 64.; 70.; 68.;|] ;   
    |]

let contrastMatrixDsd = contrastMatrix
    //[| 
     //[|1.0;-1.0;0.0;|] ; 
     //[|1.0;0.0;-1.0;|] ; 
     //[|0.0;1.0;-1.0;|]   
    //|]

dsd |> Array.map Array.average   
   
Anova.oneWayAnova dsd

TukeyHSD contrastMatrixDsd dsd 



let d1 = [159.;179.;100.;45.;384.;230.;100.;320.;80.;220.;320.;210.;]
let d2 = [14.4;15.2;11.3;2.5;22.7;14.9;1.41;15.81;4.19;15.39;17.25;9.52; ]
    

Testing.FisherHotelling.test d1 d2


// https://www.wessa.net/rwasp_Two%20Factor%20ANOVA.wasp

let data =
    [
        (0.28, 'A', 'M');
        (0.95, 'A', 'M');
        (0.96, 'A', 'M');
        (0.97, 'A', 'M');
        (0.40, 'A', 'M');
        (0.18, 'A', 'M');
        (0.12, 'A', 'M');
        (0.62, 'A', 'M');
        (1.81, 'A', 'F');
        (1.51, 'A', 'F');
        (1.41, 'A', 'F');
        (1.39, 'A', 'F');
        (1.20, 'A', 'F');
        (1.55, 'A', 'F');
        (1.48, 'A', 'F');
        (1.25, 'A', 'F');
        (0.95, 'B', 'M');
        (1.33, 'B', 'M');
        (0.92, 'B', 'M');
        (0.85, 'B', 'M');
        (1.06, 'B', 'M');
        (0.69, 'B', 'M');
        (0.70, 'B', 'M');
        (0.79, 'B', 'M');
        (2.93, 'B', 'F');
        (3.24, 'B', 'F');
        (3.42, 'B', 'F');
        (2.79, 'B', 'F');
        (2.54, 'B', 'F');
        (3.28, 'B', 'F');
        (2.80, 'B', 'F');
        (3.40, 'B', 'F');
    ]
    //f1
    |> Seq.groupBy (fun (v,f1,f2) -> f1)
    //f2
    |> Seq.map (fun (k,vls) -> 
        vls 
        |> Seq.groupBy (fun (v,f1,f2) -> f2)
        |> Seq.map (fun (k,vls') -> vls' |> Seq.map (fun (v,f1,f2) -> v) |> Seq.toArray)
        |> Seq.toArray
        ) 
    |> Seq.toArray
    

Anova.twoWayANOVA Anova.TwoWayAnovaModel.Mixed data


// http://statweb.stanford.edu/~susan/courses/s141/exanova.pdf
// http://scistatcalc.blogspot.de/2013/11/two-factor-anova-test-calculator.html#

let data' =
    [|
      // super
        // cold super
      [|[|4.;5.;6.;5.;|];
        // warm super
        [|7.;9.;8.;12.;|];
        // hot super
        [|10.;12.;11.;9.; |]|];
      // best 
        // cold best
      [|[|6.;6.;4.;4.;|];
        // warm best
        [|13.;15.;12.;12.;|];
        // hot best
        [|12.;13.;10.;13.;|]|]
    |]

Anova.twoWayANOVA Anova.TwoWayAnovaModel.Mixed data'




//qValue
// Example
let pvalues =
    //"D:/OneDrive/Development/_Current/Pep/pvalDavidTest.txt"
    //"C:/Users/muehl/OneDrive/Development/_Current/Pep/output.txt"
    "C:/Users/muehl/OneDrive/Development/_Current/Pep/output.txt"
    |> System.IO.File.ReadLines
    |> Seq.map float 
    |> Seq.toArray


let bindBy (objArr:float[]) (arr:float[]) =
    let arr' = Array.copy arr
    let index = Array.init arr.Length id
    System.Array.Sort(objArr,index)
    for i=1 to arr'.Length-1 do
        if arr'.[index.[i]] < arr'.[index.[i-1]] then
            arr'.[index.[i]] <- arr'.[index.[i-1]]
    arr'

/// Calculates q-values from given p-values.
let ofPValues (pi0:float) (pvalues:float[]) =        
    let m0 = float pvalues.Length * pi0
    pvalues
    |> Rank.rankAverage
    |> Array.mapi (fun i r -> 
        let qval = pvalues.[i] * m0 / r  
        min qval 1.
        )
    |> bindBy pvalues


// 0.6763407 from Storey R! devtools
let pi0 = Testing.PvalueAdjust.Qvalues.pi0_Bootstrap [|0.0 ..0.05..0.9|] pvalues
let qvalues  = Testing.PvalueAdjust.Qvalues.ofPValues pi0 pvalues
//let qvalues  = ofPValues pi0 pvalues


qvalues |> Seq.filter (fun x -> x < 0.1) |> Seq.length

FSharp.Plotly.Chart.Point (pvalues,qvalues)
|> FSharp.Plotly.Chart.Show

FSharp.Plotly.Chart.Histogram( pvalues, HistNorm=FSharp.Plotly.StyleParam.HistNorm.Probability)
|> FSharp.Plotly.Chart.Show



Rank.rankAverage pvalues

Rank.rankAverage [|1.;0.2;0.2;2.;3.;2.|]

Rank.rankMin [|1.;0.2;0.2;2.;3.;2.|]
Rank.rankMax [|1.;0.2;0.2;2.;3.;2.|]




bindBy [|1.;0.2;0.2;2.;3.;2.|] [|1.;0.3;0.2;2.;3.;4.|]


let bind (arr:float[]) =
    let arr' = Array.copy arr
    for i=1 to arr'.Length-1 do
        if arr'.[i] < arr'.[i-1] then
            arr'.[i] <- arr'.[i-1]
    arr'

bind [|1.;0.2;0.2;2.;3.;2.|]
