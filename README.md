# Not Base64 URL

__Not Base64 URL__ is a C# library that provides a converter to transform values of types such as `bytes[]`, `bool`, `char`, `short`, `int`, `float`, `double`, `long`, `uint`, `ulong`, and `ushort` into Base64 URL strings using **a reordered alphabet** that is URL-safe.

**IMPORTANT!** As a result, hashes created by the converter differ from hashes created by any other standard decoder. This can impact scenarios where consistent hash values are critical, such as in data integrity checks or when storing hashes in databases. Despite this fact, the library includes a method to translate between standard Base64 and Base64 URL formats.

## Features

- **Bit-by-bit Conversion**: Unlike every other encoder, this library encodes data bit by bit.
- **Not Base64 URL Alphabet**:
  - For encoding optimization, the converter uses an alphabet consisting only of URL-safe symbols, sorted in ascending order by their ASCII code values:
 
    ```csharp
    public const string alphabet = "-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";
    ```

  - The standard Base64 alphabet is as follows:

    ```csharp
    public const string base64alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    ```

- **Advantages**: The converter immediately transforms hashes into URL format, eliminating the need to replace `+` and `/` with URL-safe symbols.
-  **Compactness**: The converter contains only 1 self contained class, so if you don't want to breed dependencies, it will be easy enough to copy it.

## Problematic
[RFC 4648](https://www.ietf.org/rfc/rfc4648.txt) says:
>The encoding process divides the input bytes into groups of three bytes (24 bits). Each group of three bytes is then treated as a 24-bit group, which is divided into four 6-bit groups. 

While this describes the general process, the specific implementation details can vary between different encoders, leading to discrepancies in how bits are arranged during encoding. And for some reason any other econder shuffles bits as you could see in [System.Converter](https://referencesource.microsoft.com/#mscorlib/system/convert.cs,2278):

```cs
private static unsafe int ConvertToBase64Array(char* outChars, byte* inData, int offset, int length, bool insertLineBreaks) {
    ...
// Line 2278
outChars[j] = base64[(inData[i]&0xfc)>>2]; // from byte: 0010 0001, this line will get 001000 for the first letter and 01 will be used for the second letter
outChars[j+1] = base64[((inData[i]&0x03)<<4) | ((inData[i+1]&0xf0)>>4)]; // for the second letter, it takes the first 2 bits from the first byte and uses them as the highest bits of the second letter
// And then the shuffling continues
outChars[j+2] = base64[((inData[i+1]&0x0f)<<2) | ((inData[i+2]&0xc0)>>6)];
outChars[j+3] = base64[(inData[i+2]&0x3f)];
...
}
```
As a result it makes very complicated to shortify hashes of any object with fixed size of bytes. For example:

- _Input_: [0b1] = [00000001]
- _Standard encoders output:_ AQ== (in bit format [000000 010000 /* '==' bits*/]) 
- _The Library encoder output:_ 0-== (in bit format [000001 000000 /* '==' bits*/])


## License

This library is distributed under the MIT License, allowing for free use, modification, and distribution in both personal and commercial projects.