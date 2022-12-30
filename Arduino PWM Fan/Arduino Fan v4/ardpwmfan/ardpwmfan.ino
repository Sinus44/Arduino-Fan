#include <GyverPWM.h>

// Generals settings
#define pin 3               // Пин к которому подключен вентилятор
#define serialSpeed 9600   // Скорость передачи
#define serialTimeout 100 // максимальное время ожидания конца

//PWM
#define PWM_bits 4        // Разрядность ШИМ
#define PWM_start 5      // Минимальное выходное значение ШИМ

void setup() {
  Serial.begin(serialSpeed);
  Serial.setTimeout(serialTimeout);
  pinMode(pin, OUTPUT);
  PWM_resolution(pin, PWM_bits, FAST_PWM);
  PWM_set(pin, PWM_start);
}

int data;
float pwm;

void loop() {
  if (Serial.available()) {
    char key = Serial.read();
    switch (key) {
      case 'f':
        data = Serial.parseInt();

        if (data == 0) {
          pwm = 0;
        } else {
          pwm = data / 100 * (pow(2, PWM_bits) - 1 - PWM_start) + PWM_start;
        }
        PWM_set(pin, (int)pwm);

        break;
      case 'p':

          Serial.print("p");

          break;
        }
  }
}
