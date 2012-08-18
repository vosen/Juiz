#I "bin\\Release"
#r "FunkSVD"
#r "FunkSVD.RMSE"
#r "MathNet.Numerics"
#r "OxyPlot"
#r "OxyPlot.WindowsForms"

let tuplify (twoElArr : string array) =
    (int(twoElArr.[0]), float(twoElArr.[1]))

let loadData path =
    let lines = System.IO.File.ReadAllLines(path)
    lines
    |> Array.filter (fun line -> not (System.String.IsNullOrWhiteSpace(line)))
    |> Array.map (fun line -> line.Split([|','|]) |> tuplify)
    //let lines = System.String.Split(data)

let avgLineSeries min max avg =
    let series = OxyPlot.LineSeries()
    series.Points <- Array.init (max-min+1) (fun i -> OxyPlot.DataPoint(float(i), avg) :> OxyPlot.IDataPoint)
    series.StrokeThickness <- 1.0
    series.Color <- OxyPlot.OxyColors.Pink
    series

let buildSeries (data, color) =
    let series = OxyPlot.LineSeries()
    series.Points <- data |> Array.map (fun rating -> OxyPlot.DataPoint(float(fst rating), snd rating) :> OxyPlot.IDataPoint)
    series.MarkerType <- OxyPlot.MarkerType.Cross
    series.StrokeThickness <- 0.0
    series.MarkerStroke <- color;
    series

let exportDataGraph data path =
    let model = OxyPlot.PlotModel("RMSE in relation to features")
    let series = OxyPlot.LineSeries()
    series.Points <- data |> Array.map (fun rating -> OxyPlot.DataPoint(float(fst rating), snd rating) :> OxyPlot.IDataPoint)
    series.MarkerType <- OxyPlot.MarkerType.Cross
    series.StrokeThickness <- 0.0
    series.MarkerStroke <- OxyPlot.OxyColors.Black;
    series.MarkerSize <- 4.0
    model.Series.Add(series)
    OxyPlot.WindowsForms.PngExporter.Export(model, path + ".png", 800, 600, System.Drawing.Brushes.White)

let graph paths colors (model : OxyPlot.PlotModel) =
    let series = (paths |> Array.map loadData, colors) ||> Array.zip |> Array.map buildSeries
    for ser in series do
        model.Series.Add(ser)
    OxyPlot.WindowsForms.PngExporter.Export(model, "graph_compare.png", 800, 600, System.Drawing.Brushes.White)

let graphWithAvg paths colors (model : OxyPlot.PlotModel) =
    let series = (paths |> Array.map loadData, colors) ||> Array.zip |> Array.map buildSeries |> Array.append [| (avgLineSeries 10 200 1.718746) |]
    for ser in series do
        model.Series.Add(ser)
    OxyPlot.WindowsForms.PngExporter.Export(model, "graph_compare.png_avg", 800, 600, System.Drawing.Brushes.White)

let paths = [| "..\\FunkSVD.RMSE\\bin\\Release\\result-fix3-raw.txt"; "..\\FunkSVD.RMSE\\bin\\Release\\result-avgfeat-raw.txt"; "..\\FunkSVD.RMSE\\bin\\Release\\result-avgfeat-nomin-raw.txt"; |]
let colors = [| OxyPlot.OxyColors.Blue; OxyPlot.OxyColors.Red; OxyPlot.OxyColors.Green; |]
let model = OxyPlot.PlotModel("RMSE in relation to features")
graph paths colors model
graphWithAvg paths colors model



