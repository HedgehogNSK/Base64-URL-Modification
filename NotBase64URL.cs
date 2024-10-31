using System.Text;

namespace Hedgey.Utilities;

static public class NotBase64URL
{
  //[\-0-9A-Z_a-Z] [45;[48;57];[65;90];95;[97;122]]
  public const string alphabet = "-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
  //[A-Za-Z0-9+/] [[65;90];[97;122];[48;57];43;47]
  public const string base64alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

  const uint intTab = '\t';   //9u
  const uint intNLn = '\n';   //10u
  const uint intCRt = '\r';   //13u
  const uint intPlus = '+';   //43u
  const uint intDash = '-';   //45u  -> 0
  const uint intSlash = '/';  //47u
  const uint intColon = ':';  //58u
  const uint intEq = '=';     //61u
  const uint intSub = '_';    //95u -> 37
  static public unsafe byte[] GetBytes(string hash)
  {
    const uint int0 = '0' - 1u; //47u 
    const uint intA = 'A' - 1u; //64u 
    const uint inta = 'a' - 1u; //96u  

    int bytesLength = hash.Length - (hash.Length >> 2); // = 3/4 * length
    int switcher = 0;
    unchecked
    {
      byte* bytePtr = stackalloc byte[bytesLength];
      byte* byteBeginPtr = bytePtr;
      fixed (char* charPtr = hash)
      {

        char* endPtr = charPtr + hash.Length;
        for (char* ptr = charPtr; ptr != endPtr; ++ptr)
        {
          uint currCode = *ptr;

          // Determine current char code:
          //intDash has code 000000. So we don't need to write anything. Just move pointers to next bits
          if (currCode == intDash)
          {
            if (switcher != 0)
              ++bytePtr;

            ++switcher;
            switcher &= 3;
          }
          else
          {
            if (currCode > inta)
              currCode -= 59u;//[a;z] : [97;122] -> [38;63] diff = 59
            else if (currCode == intSub)
              currCode = 37u;
            else if (currCode > intA)
              currCode -= 54u; //[A;Z] : [65;90]  -> [11;36] diff = 54
            else if (currCode > int0)
              currCode -= 47u; //['0';'9'] : [48;57]  -> [1;10]  diff = 47

            else if (currCode == intTab
                  || currCode == intNLn
                  || currCode == intCRt)
            {
              ++ptr;
              continue;
            }
            else if (currCode == intEq)
            {
              break;
            }
            else
              throw new FormatException($"Symbol \"{*ptr}\"[code:{currCode}] doesn't belong to Base64URL");

            //Distribute 4 letters by 6 bits into 3 bytes one  by one
            //[000000] [00 0000] [0000 00] [000000]
            // 1           2           3
            if (switcher == 0)
            {
              *bytePtr |= (byte)currCode;
              ++switcher;
            }
            else if (switcher == 1)
            {
              *bytePtr |= (byte)(currCode << 6);
              ++bytePtr;
              *bytePtr |= (byte)(currCode >> 2);
              ++switcher;
            }
            else if (switcher == 2)
            {
              *bytePtr |= (byte)(currCode << 4);
              ++bytePtr;
              *bytePtr |= (byte)(currCode >> 4);
              ++switcher;
            }
            else
            {
              *bytePtr |= (byte)(currCode << 2);
              ++bytePtr;
              switcher = 0;
            }
          }
        }
      }

      byte[] array = new byte[bytePtr - byteBeginPtr];
      int id = 0;
      while (byteBeginPtr != bytePtr)
      {
        array[id++] = *byteBeginPtr;
        ++byteBeginPtr;
      }
      return array;
    }
  }
  public static string From(byte[] bytes)
  {
    //Each 3 bytes translates into 4 letters from the list
    int length = bytes.Length;
    int stringLength = ((length + 2) / 3) << 2;

    char[] result = new char[stringLength];

    int resultIndex = 0;
    int index = 0;

    //Distribute 4 letters by 6 bits into 3 bytes one  by one
    //[000000] [00 0000] [0000 00] [000000]
    while (index < length - 2)
    {
      byte b1 = bytes[index++];
      byte b2 = bytes[index++];
      byte b3 = bytes[index++];

      result[resultIndex++] = alphabet[b1 & 0x3F]; //0x3F = 1111 1100 (Big Ending)
      result[resultIndex++] = alphabet[(b1 >> 6) | ((b2 << 2) & 0x3C)]; //0x3C 0011 1100
      result[resultIndex++] = alphabet[(b2 >> 4) | ((b3 << 4) & 0x30)]; //0x30 0000 1100
      result[resultIndex++] = alphabet[(b3 >> 2) & 0x3F]; //0x0F 1111 0000
    }

    if (index == length) return new string(result);

    byte b4 = bytes[index++];
    result[resultIndex++] = alphabet[b4 & 0x3F]; //0x3F 1111 1100

    if (index == length)
    {
      result[resultIndex++] = alphabet[b4 >> 6];
      result[resultIndex++] = '=';
    }
    else
    {
      byte b5 = bytes[index++];
      result[resultIndex++] = alphabet[(b4 >> 6) | ((b5 << 2) & 0x3C)]; //0x3C 0011 1100
      result[resultIndex++] = alphabet[b5 >> 4];
    }
    result[resultIndex++] = '=';

    return new string(result);
  }
  static public string From(bool value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }
  static public string From(char value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }
  static public string From(float value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }
  static public string From(double value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }
  static public string From(short value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }
  static public string From(ushort value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }

  static public string From(int value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }
  static public string From(uint value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }

  static public string From(ulong value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }
  static public string From(long value)
  {
    var bytes = BitConverter.GetBytes(value);

    if (!BitConverter.IsLittleEndian)
      Array.Reverse(bytes);
    return From(bytes);
  }
  static public long ToLong(string hash)
  {
    var bytes = GetBytes(hash);
    if (bytes.Length != 8)
      Array.Resize(ref bytes, 8);
    return BitConverter.ToInt64(bytes, 0);
  }
  /// <summary>
  /// Translates Base64 symbols into Base64 Url mod symbols without hash validation
  /// Only symbols will be validated
  /// </summary>
  /// <param name="base64hash"></param>
  /// <returns></returns>
  /// <exception cref="ArgumentException"></exception>
  static public string TranslateFromBase64(string base64hash)
  {
    const uint intl = 'l';
    const uint inty = 'y';
    const uint intz = 'z';
    const uint intA = 'A';
    StringBuilder builder = new StringBuilder();

    for (int i = 0; i != base64hash.Length; ++i)
    {
      char c = base64hash[i];
      uint charNumber = c;
      if (charNumber > 96u) //a - z: 97- 122
      {
        if (charNumber < intl) // [a;k]
                               // a - P : 97 - 80  = 17
          charNumber -= 17u;
        else if (charNumber == intl)
          charNumber = intSub;
        else if (charNumber < 123u) //[l;z]
          // m - a : 109 - 97 = 12
          charNumber -= 12u;
        else
          NotBase64Symbol(c, base64hash);
      }
      else if (charNumber > 64u) //A = 65u [A;Z]
      {
        if (charNumber == intA)
          charNumber = intDash;
        else if (charNumber < 76u) //L
          // B - 0 : 66 - 48 = 18
          charNumber -= 18u;
        else if (charNumber < 91u)
          // L - A : 76 - 65 = 11
          charNumber -= 11u;
        else
          NotBase64Symbol(c, base64hash);
      }
      else if (charNumber > intSlash) /*[0;9] + "="*/
      {
        if (charNumber < intColon)
          // 0 - o : 48 - 111 = -63u
          charNumber += 63u;
        else if (charNumber != intEq)
          NotBase64Symbol(c, base64hash);
      }
      else if (charNumber == intSlash)
        charNumber = intz;
      else if (charNumber == intPlus)
        charNumber = inty;
      else if (charNumber != intEq && charNumber != intTab && charNumber != intNLn && charNumber != intCRt)
        NotBase64Symbol(c, base64hash);

      c = (char)charNumber;
      builder.Append(c);
    }
    return builder.ToString();
  }

  static private void NotBase64Symbol(char c, string hash)
  {
    throw new ArgumentException($"Symbol \'{c}\' doesn't belong to Base64 symbols. Therefore it's not base64 hash: \'{hash}\'");
  }
}