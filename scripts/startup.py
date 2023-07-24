# set to run at startup with rc.local to display the Pi's IP address to the LCD

from time import sleep
from subprocess import check_output
from RPLCD.i2c import CharLCD

sleep(10)
lcd = CharLCD(i2c_expander='PCF8574', address=0x27)
msg = check_output(['hostname', '-I']).decode("utf-8").strip()
lcd.write_string(msg)
