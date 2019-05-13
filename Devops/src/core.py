import os
import uuid
import time
import pandas as pd
from datetime import datetime as dt
from sklearn.externals import joblib
from sklearn.pipeline import Pipeline
from sklearn.neighbors import NearestNeighbors
from sklearn.feature_extraction.text import TfidfVectorizer
from azure.storage.common import CloudStorageAccount
from applicationinsights import TelemetryClient


SCORING_MODEL_FOLDER = "asset"
SCORING_MODEL_NAME = "scoring_model.pkl"
SCORING_MODEL_PATH = os.path.join(SCORING_MODEL_FOLDER, SCORING_MODEL_NAME)

TRAINING_FOLDER = "data"


def GetTelemetry() -> TelemetryClient:
    appKey = os.environ.get("APPINSIGHTS_INSTRUMENTATIONKEY")
    tc = TelemetryClient(appKey)
    return tc


class ScoringContext:
    def __init__(self, model, vectorizer, refFile):

        self.vectorizer = vectorizer
        self.model = model
        self.refFile = refFile


class AppContext:
    def __init__(self):

        self.container = os.environ.get("STORAGEACCOUNT_CONTAINER_NAME")

        self.account = CloudStorageAccount(
            account_name=os.environ.get("STORAGEACCOUNT_NAME"),
            account_key=os.environ.get("STORAGEACCOUNT_KEY"),
        )

    def ModelExists(self) -> bool:
        return os.path.exists(SCORING_MODEL_PATH)

    def LoadModel(self):

        if not self.ModelExists():
            return None

        return joblib.load(SCORING_MODEL_PATH)

    def DownloadTrainingFile(self, tweetSourceFileName: str) -> str:

        ds = self.account.create_block_blob_service()

        if not ds.exists(self.container, tweetSourceFileName):
            return None

        if not os.path.exists(TRAINING_FOLDER):
            os.makedirs(TRAINING_FOLDER)

        localFilePath = os.path.join(
            TRAINING_FOLDER, os.path.basename(tweetSourceFileName)
        )
        ds.get_blob_to_path(self.container, tweetSourceFileName, localFilePath)

        return localFilePath

    def Train(self, tweetSourceFileName: str):

        startTime =time.time()
        localFile = self.DownloadTrainingFile(tweetSourceFileName)
        df = pd.read_json(localFile)
        if len(df) == 0:
            return None

        contentVectorizer = TfidfVectorizer(
            use_idf=True, analyzer="char", ngram_range=(3, 3)
        )
        ml_model = NearestNeighbors(n_neighbors=3, algorithm="brute", metric="cosine")

        preprocessing_pipeline = Pipeline(steps=[("Vectorize", contentVectorizer)])

        ml_pipeline = Pipeline(
            steps=[
                ("preprocessing", preprocessing_pipeline),
                ("machineLearning", ml_model),
            ]
        )

        ml_pipeline.fit(df["Text"])

        def Eval(frame):

            df = frame[:1000]
            count = 0
            for i, row in df.iterrows():
                tweet = row.Text
                vector_to_search = preprocessing_pipeline.transform([tweet])
                distances, indices = ml_model.kneighbors(
                    vector_to_search, n_neighbors=3
                )

                if i in indices[0]:
                    count += 1

            return count / len(df) * 100

        precision = Eval(df)
        duration = time.time()-startTime
        tc = GetTelemetry()
        tc.track_event(
            "Train",
            {
                "Source": tweetSourceFileName
            },
            {
                "Precision": precision,
                "Count": df.size.item(),
                "Duration": duration
            },
        )
        tc.flush()

        ctx = ScoringContext(ml_model, contentVectorizer, localFile)

        self.saveModel(ctx)

        return ctx

    def saveModel(self, ctx):

        if not os.path.exists(SCORING_MODEL_FOLDER):
            os.makedirs(SCORING_MODEL_FOLDER)

        joblib.dump(ctx, SCORING_MODEL_PATH)

    def FindMostSimilar(self, text, topN=3):
        startTime =time.time()
        ctx = self.LoadModel()
        vector_to_search = ctx.vectorizer.transform([text])
        df = pd.read_json(ctx.refFile)
        distances, indices = ctx.model.kneighbors(vector_to_search, n_neighbors=topN)
        binomes = list(zip(indices[0], distances[0]))
        result = []

        maxScore = 0
        for i in range(topN):
            row = df.iloc[indices[0][i]]
            score = 1 - distances[0][i]
            if i == 0:
                maxScore = score
            statusID = row["StatusID"]
            result.append(
                {
                    "Id": str(statusID),
                    "Content": row["Text"],
                    "CreatedAt": row["CreatedAt"],
                    "MatchingScore": score,
                }
            )
        duration = time.time()-startTime
        tc = GetTelemetry()
        tc.track_event("Search", {"Request": text}, {"MaxScore": maxScore, "Duration": duration})
        tc.flush()
        return result

