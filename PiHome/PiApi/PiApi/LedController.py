"""
Routes and views for the flask application.
"""

from PiApi import app, settings
from Apa102 import Manager
from flask import request, jsonify

manager = Manager.Manager(settings.numLeds)

@app.route('/led/api/turnoff', methods=['GET'])
def turnoff():
    manager.shutdown()
    return jsonify({'status': 'success'})

@app.route('/led/api/solid/<int:red>/<int:green>/<int:blue>', methods=['GET', 'PUT'])
def solid(red, green, blue):
    manager.SetSolidAndRender(red, green, blue)
    return jsonify({'status': 'success'})

@app.route('/led/api/solid', methods=['POST'])
def solidFromBody():
    json = request.get_json(force = True)
    if not 'red' in json:
        red = json.get('r', 0)
    else:
        red = json.get('red', 0)
    if not 'green' in json:
        green = json.get('g', 0)
    else:
        green = json.get('green', 0)
    if not 'blue' in json:
        blue = json.get('b', 0)
    else:
        blue = json.get('blue', 0)
    if not 'brightness' in json:
        brightness = json.get('a', 0)
    else:
        brightness = json.get('brightness', 0)
    manager.SetSolidAndRender(red, green, blue, brightness)
    return jsonify({'status': 'success'})
    
@app.route('/led/api/customRGB', methods=['POST'])
def customRGB():
    data = bytes(request.get_data())
    manager.SetAllPixelsRGBAndRender(data)
    return jsonify({'status': 'success'})
    
@app.route('/led/api/customRGBB', methods=['POST'])
def customRGBB():
    data = bytes(request.get_data())
    manager.SetAllPixelsRGBBAndRender(data)
    return jsonify({'status': 'success'})
