
from PiApi import app, settings
from os import environ
from Pozyx import Controller
from flask import Request, jsonify

pozyx = Controller.Controller(settings.Anchors)

@app.route('/position/api/configure/anchor/<string:anchorId>', methods=['GET'])
def configureAnchor(anchorId):
    numericAnchorId = int(anchorId, 0)
    pozyx.ConfigureAnchor(numericAnchorId)
    return jsonify({'status': 'success'})

@app.route('/position/api/configure/tag/<string:tagId>', methods=['GET'])
def configureTag(tagId):
    numericTagId = int(tagId, 0)
    pozyx.ConfigureTag(numericTagId)
    return jsonify({'status': 'success'})

@app.route('/position/api/configure/uwb/<string:tagId>', methods=['POST'])
def configureAnchorUwb(tagId):
    numericTagId = int(tagId, 0)
    son = Request.get_json(force = True)
    if 'txPower' in json:
        txPower = json.get('txPower', 33.0)
    if 'channel' in json:
        channel = json.get('channel', 1)
    if 'bitrate' in json:
        bitrate = json.get('bitrate', 0)
    if 'gain' in json:
        gain = json.get('gain', 67)
    if 'prf' in json:
        prf = json.get('prf', 2)
    if 'preamble' in json:
        preamble = json.get('preamble', 40)
    pozyx.ConfigureUWB(numericTagId, txPower, channel, bitrate, gain, prf, preamble)
    return jsonify({'status': 'success'})

@app.route('/position/api/discover', methods=['GET'])
def discoverTags():
    ids = pozyx.DiscoverTags()
    return jsonify(ids)

@app.route('/position/api/calibrationStatus/<string:tagId>', methods=['GET'])
def calibrationStatus(tagId):
    numericTagId = int(tagId, 0)
    status = pozyx.GetCalibrationStatus(numericTagId)
    return jsonify(status)

@app.route('/position/api/error', methods=['GET'])
def getError():
    status = pozyx.GetError()
    return jsonify({'error': status})

@app.route('/position/api/eulerAngle/<string:tagId>', methods=['GET'])
def getAngles(tagId):
    numericTagId = int(tagId, 0)
    status = pozyx.GetEulerAngles(numericTagId)
    return jsonify(status)

@app.route('/position/api/gpio/<string:tagId>/<int:pinId>', methods=['GET'])
def getGpio(tagId, pinId):
    numericTagId = int(tagId, 0)
    status = pozyx.GetGPIO(numericTagId, pinId)
    return jsonify({'Pin' + str(pinId) : status})

@app.route('/position/api/gpio/<string:tagId>', methods=['GET'])
def getAllGpio(tagId):
    numericTagId = int(tagId, 0)
    status1 = pozyx.GetGPIO(numericTagId, 1)
    status2 = pozyx.GetGPIO(numericTagId, 2)
    status3 = pozyx.GetGPIO(numericTagId, 3)
    status4 = pozyx.GetGPIO(numericTagId, 4)
    return jsonify({'Pin1': status1, 'Pin2': status2, 'Pin3': status3, 'Pin4': status4})

@app.route('/position/api/position/<string:tagId>', methods=['GET'])
def getPosition(tagId):
    numericTagId = int(tagId, 0)
    result = pozyx.GetPosition(numericTagId)
    return jsonify(result)

@app.route('/position/api/pressure/<string:tagId>', methods=['GET'])
def getPressure(tagId):
    numericTagId = int(tagId, 0)
    result = pozyx.GetPressure(numericTagId)
    return jsonify(result)

@app.route('/position/api/temperature/<string:tagId>', methods=['GET'])
def getTemperature(tagId):
    numericTagId = int(tagId, 0)
    result = pozyx.GetTemperature(numericTagId)
    return jsonify(result)
