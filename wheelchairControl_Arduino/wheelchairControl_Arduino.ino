// Digital pin #2 is the same as Pin D2 see
// http://arduino.cc/en/Hacking/PinMapping168 for the 'raw' pin mapping
#define DATA_PIN_REG PIND
#define DATA_DDR_REG DDRD
#define DATA_PORT_REG PORTD
#define DATA_PIN 2
#define BIT_WIDTH 26 //uS
#define DELAY_CMD 8 //ms

#define NUM_PACKETS 6
#define LEN_PACKET (3+8+1)
#define LEN_CMD (LEN_PACKET*NUM_PACKETS)

#define CMD_INIT   "11001001010111010110101111."

#define RX_TIMEOUT 500

char cmd_buf[LEN_CMD];

char pow2(int p)
{
  int retval = 1;
  for (int i = 0; i < p; i++)
    retval *= 2;
  return retval;
}

//
void charToString(const char val, char *string)
{
 for (int i = 0; i < 8; i++)
   string[i] = (val & pow2(i)) ? '1' : '0';
}

//
void charToStringI(const char val, char *string)
{
 for (int i = 0; i < 8; i++)
   string[i] = (val & pow2(i)) ? '0' : '1';
}

//
void stringToChar(const char *string, char &val)
{
  val = 0;
 for (int i = 0; i < 8; i++)
  if (string[i] == '1')
   val += pow2(i);
}

//example:
//    start          horn           speed[4]       y              x              checksum     
//110 01010010 1 110 10000000 1 110 11100000 1 110 00101011 0 110 01000000 1 110 11101011 0 
void buildCmd(char *buf, char speed, char y, char x)
{
  char tmp[8];
  
  //packet starts
  for (int p = 0; p < NUM_PACKETS; p++)
  {
    buf[p*LEN_PACKET + 0] = '1';
    buf[p*LEN_PACKET + 1] = '1';
    buf[p*LEN_PACKET + 2] = '0';
  }
  
  //start packet
  memcpy(buf + (0*LEN_PACKET + 3), "01010010", 8);
  
  //horn packet
  memcpy(buf + (1*LEN_PACKET + 3), "10000000", 8);
  
  //speed packet
  charToString(speed, tmp);
  memcpy(buf + (2*LEN_PACKET + 3), tmp + 4, 4);
  memcpy(buf + (2*LEN_PACKET + 3) + 4, "0000", 4);
  
  //y packet
  charToString(y, tmp);
  memcpy(buf + (3*LEN_PACKET + 3), tmp, 8);

  //x packet
  charToString(x, tmp);
  memcpy(buf + (4*LEN_PACKET + 3), tmp, 8);
  
  //checksum
  char checkVal = 0;
  for (int p = 0; p < NUM_PACKETS - 1; p++)
  {
    char val;
    stringToChar(buf + (p*LEN_PACKET + 3), val);
    checkVal += val;
  }
  charToStringI(checkVal, buf + ((NUM_PACKETS - 1) * LEN_PACKET + 3));
  
  //packet parity
  for (int p = 0; p < NUM_PACKETS; p++)
  {
    bool even = true;
    for (int b = 0; b < 8; b++)
      if (buf[p*LEN_PACKET + 3 + b] == '1')
        even = !even;
    
    buf[p*LEN_PACKET + LEN_PACKET - 1] = even ? '0' : '1';
  }  
}

//
uint32_t lastTx;
void send(const char *bits)
{
  lastTx = millis();
  cli();
  DATA_DDR_REG |= (1 << DATA_PIN);

  for (int b = 0; b < LEN_CMD; b++)
  {
    if (*bits == '0')
      DATA_PORT_REG &= ~(1 << DATA_PIN);//lo
    else if (*bits == '1')
      DATA_PORT_REG |= (1 << DATA_PIN);//hi
    else
      break;
      
    delayMicroseconds(BIT_WIDTH);
    bits++;
  }

  DATA_PORT_REG |= (1 << DATA_PIN);//hi
  delayMicroseconds(BIT_WIDTH * 6);

  DATA_DDR_REG &= ~(1 << DATA_PIN);//release
  DATA_PORT_REG &= ~(1 << DATA_PIN);
  sei();
}

//
void setup()
{
  //setup wireless  
  Serial.begin(115200); 
  
  //send start sequence
  for (int i = 0; i < 100; i++)
  {
    send(CMD_INIT);
    delay(DELAY_CMD);
  }
  
  char speed = 10;
  
  buildCmd(cmd_buf, speed, 0, 0);//init
  
  //process wireless commands
  uint32_t lastRx;
  char ud, lr;
  while (1)
  {
    if (Serial.available())
    {
      char rxData = Serial.read();

      switch (rxData)
      {
          case 'u': buildCmd(cmd_buf, speed, 100, 0); break;
          case 'd': buildCmd(cmd_buf, speed, -100, 0); break;
          case 'l': buildCmd(cmd_buf, speed, 0, -100); break;
          case 'r': buildCmd(cmd_buf, speed, 0, 100); break;
          case '\r':
          case '\n':
              break;
          default: buildCmd(cmd_buf, speed, 0, 0); break;
      }

      lastRx = millis();
    }

    //disable motor
    if (millis() - lastRx > RX_TIMEOUT)
    {
        buildCmd(cmd_buf, speed, 0, 0);
    }   

    if (millis() - lastTx > DELAY_CMD)
    {
      send(cmd_buf);
    }
  }
  
  while (1) {}
}

//
void loop()
{
}

