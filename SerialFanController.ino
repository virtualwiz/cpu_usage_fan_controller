#define MA1 3 
#define MA2 5
#define MB1 9
#define MB2 6

#define LOWPASS_WINDOW_LENGTH 10
int value_buf[LOWPASS_WINDOW_LENGTH]; 

int Filter(int Data_In) 
{
	static char i = 0;
	char count; 
	int sum=0; 
	value_buf[i++] = Data_In; 
	if ( i == LOWPASS_WINDOW_LENGTH )   
		i = 0;
	for ( count=0 ; count < LOWPASS_WINDOW_LENGTH ; count++) 
		sum += value_buf[count];
	return (int)(sum/LOWPASS_WINDOW_LENGTH); 
}

void GPIO_Init()
{
	pinMode(MA1,OUTPUT);
	pinMode(MA2,OUTPUT);
	pinMode(MB1,OUTPUT);
	pinMode(MB2,OUTPUT);
	digitalWrite(MA2,LOW);
	digitalWrite(MB1,LOW);
}

void Motor_SetSpeed(unsigned char speedPercentage)
{
	unsigned char speedByte = map(speedPercentage,0,100,1,255);
	analogWrite(MB2,speedByte);
}

void setup()
{
	GPIO_Init();
        TCCR0B = TCCR0B & B11111000 | B00000010;
        Motor_SetSpeed(100);
        delay(100);
        Motor_SetSpeed(1);
        Serial.begin(9600);
}

unsigned char speedRcv = 0;

void loop()
{
	if(Serial.available())
	{
		speedRcv = Serial.read();
		Motor_SetSpeed(Filter(speedRcv));
	}
}
