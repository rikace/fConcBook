namespace TwitterEmotionAnalysis.FSharp

module MLearning =
    
    open System
    open System.IO
    open Microsoft.ML
    open Microsoft.ML.Data

    type SentimentData () =
        [<DefaultValue>]
        [<LoadColumn(0)>]
        val mutable public SentimentText :string

        [<DefaultValue>]
        [<LoadColumn(1)>]
        val mutable public Label :bool
        
        // NOTE: Need to add this column to extract metrics
        [<DefaultValue>]
        [<LoadColumn(2)>]
        val mutable public Probability :float32

    type SentimentPrediction () =
        [<DefaultValue>]
        val mutable public SentimentData :string

        [<DefaultValue>]
        val mutable public PredictedLabel :bool

        [<DefaultValue>]
        val mutable public Score :float32 

    let initPredictor (dataFile:string) =
        let ml = new MLContext()
        let reader = ml.Data.CreateTextLoader<SentimentData>(separatorChar = '\t', hasHeader = true)
      
        let allData = reader.Load(dataFile)
        let data = ml.Data.TrainTestSplit(allData, testFraction = 0.3)
        
        //let estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentData.SentimentText))
        
        let reader = 
          ml.Data.CreateTextLoader(
            separatorChar = '\t',
            hasHeader = true,
            columns = 
              [|
                  Data.TextLoader.Column("SentimentText", Data.DataKind.String, 0);
                  Data.TextLoader.Column("Label", Data.DataKind.Boolean, 1);                  
                  Data.TextLoader.Column("Probability", Data.DataKind.Double, 2)
              |])
    
        let pipeline =
          ml // (numTrees = 500, numLeaves = 100, learningRate = 0.0001)
            .Transforms.Text.FeaturizeText("SentimentText", "Features")
            .Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression())
            .Append(ml.BinaryClassification.Trainers.LbfgsLogisticRegression())
            
        let model = pipeline.Fit(data.TrainSet)

        ml.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model)
    
    let saveModel (ml:MLContext) schema model (path:string) =        
        use fsWrite = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write)
        ml.Model.Save(model, schema, fsWrite)
      
    // Load model from file
    let loadModel (path:string) =        
        use fsRead = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
        let mlReloaded = MLContext()
        let transformer, schema = mlReloaded.Model.Load(fsRead)   
        mlReloaded.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(transformer)
        

    let runPrediction (model:PredictionEngine<SentimentData, SentimentPrediction>) (testText:string) =
        let test = SentimentData()
        test.SentimentText <- testText
        model.Predict(test)
        
    let scorePrediction (prediction:SentimentPrediction) = 
        printfn "score : %O"  prediction.Score
        prediction.Score
