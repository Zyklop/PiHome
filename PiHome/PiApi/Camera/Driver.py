
from picamera import PiCamera

class Driver(object):

    def __init__(self, path, number):
        self.basebath = path
        self.camera = PiCamera(resolution=(3264,2448))
        self.nbr = number

    def takePicture(self):
        path = self.basebath + 'img' + str(self.nbr) + '.jpg'
        self.str += 1
        self.camera.capture(path)

    def __del__(self):
        camera.close()