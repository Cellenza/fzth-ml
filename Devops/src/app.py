import time
import os
from flask import Flask, request, jsonify, Response, g
from core import AppContext, GetTelemetry
from applicationinsights.flask.ext import AppInsights

app = Flask(__name__)
app.config['APPINSIGHTS_INSTRUMENTATIONKEY'] = os.environ.get('APPINSIGHTS_INSTRUMENTATIONKEY')
AppInsights(app)


@app.route("/")
def index():
    return jsonify("Good Job. The server is running well")


@app.route("/train")
def train():

    filename = request.args.get("filename")

    if not filename:

        return jsonify(
            {"ErrorMessage": "training file path is mandatory", "StatusCode": 400}
        )

    ret = AppContext().Train(filename)

    if not ret:

        return jsonify(
            {"ErrorMessage": "The training file is missing or empty", "StatusCode": 400}
        )

    return jsonify("Server done training")


@app.route("/search", methods=["GET"])
def search():

    searchText = request.args.get("q")

    if not searchText:

        errorResponse = jsonify(
            {"ErrorMessage": "Missing Search text", "StatusCode": 400}
        )

        return errorResponse, 400

    tweets = AppContext().FindMostSimilar(searchText)

    resp = jsonify(tweets)
    resp.headers["Access-Control-Allow-Origin"] = "*"

    return resp


@app.route("/check")
def CheckService():

    if (AppContext().ModelExists()):
        currentState = "ok"
    else:
        currentState = "needTraining"

    resp = {
        "currentState":currentState,
        "possibleStates": {
            "ok": " The service is running",           
            "needTraining": "The service is not trained yet or might be broken",
        },
    }

    resp = jsonify(resp)
    resp.headers["Access-Control-Allow-Origin"] = "*"

    return resp


if __name__ == "__main__":
    app.run(debug=True, host="0.0.0.0", port=8000)
