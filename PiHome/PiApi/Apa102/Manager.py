from Apa102 import Driver

class Manager(object):
    """Manages Apa102 Object"""

    def __init__(self, numLeds):
        self.NumLeds = numLeds
        self.IsStarted = False

    def startup(self):
        self.strip = Driver.Driver(num_led=self.NumLeds, global_brightness=31, mosi = 10, sclk = 11, order='rgb')
        self.strip.clear_strip()
        self.IsStarted = True

    def shutdown(self):
        if not self.IsStarted:
            return
        self.IsStarted = False
        self.strip.clear_strip()
        self.strip.cleanup()

    def check(self):
        if not self.IsStarted:
            self.startup()

    def SetPixel(self, index, r, g, b, bright_percent=100):
        self.check()
        self.strip,SetPixel(index, r, g, b, bright_percent)

    def Render(self):
        self.check()
        self.strip.show()

    def SetAllPixelsRGBAndRender(self, data):
        self.check()
        for led in range(self.NumLeds):
            if led < len(data) / 3:
                self.strip.set_pixel(led, data[led * 3], data[led * 3 + 1], data[led * 3 + 2])
            else:
                self.strip.set_pixel(led, 0, 0, 0)
        self.strip.show()

    def SetAllPixelsRGBBAndRender(self, data):
        self.check()
        for led in range(self.NumLeds):
            if led < len(data) / 4:
                self.strip.set_pixel(led, data[led * 4], data[led * 4 + 1], data[led * 4 + 2], data[led * 4 + 3])
            else:
                self.strip.set_pixel(led, 0, 0, 0)
        self.strip.show()

    def SetSolidAndRender(self, r, g, b, bright_percent=100):
        self.check()
        for led in range(self.NumLeds):
            self.strip.set_pixel(led, r, g, b, bright_percent)
        self.strip.show()
