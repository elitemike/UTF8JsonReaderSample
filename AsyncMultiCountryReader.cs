using ReaderSample;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonReader
{
    public class AsyncMultiCountryReader
    {
        public byte[] buffer = new byte[2_048];
        public int leftOver = 0;
        public long bytesConsumed = 0;
        public Stream stream;
        public JsonReaderState state = default;

        public AsyncMultiCountryReader()
        {

            stream = new FileStream("multi_country_universities_slim.json", FileMode.Open);
        }

       
        public async IAsyncEnumerable<Country> ReadCountries()
        {
            Country country;

            while (true)
            {
                int dataLength = await stream.ReadAsync(buffer.AsMemory(leftOver, buffer.Length - leftOver));
                int dataSize = dataLength + leftOver;
                bool isFinalBlock = dataSize == 0;

                (country, bytesConsumed) = ReadCountry(buffer.AsSpan(0, dataSize), isFinalBlock, ref state);

                leftOver = dataSize - (int)bytesConsumed;
                if (leftOver != 0)
                {
                    buffer.AsSpan(dataSize - leftOver, leftOver).CopyTo(buffer);
                }

                if (isFinalBlock || country == null)
                {
                    break;
                }

                yield return country;
            }
        }


        public (Country country, long bytesConsumed) ReadCountry(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, ref JsonReaderState state)
        {
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock, state);
            Country country = null;

            var needsRead = true;
            while (json.Read() && needsRead)
            {
                JsonTokenType tokenType = json.TokenType;

                switch (tokenType)
                {
                    case JsonTokenType.StartObject:
                        country = new Country(this);
                        break;
                    case JsonTokenType.PropertyName:
                        if (json.ValueSpan.SequenceEqual(Country.Name_UTF8))
                        {
                            json.Read();
                            country.Name = json.GetString();

                            needsRead = false;
                        }
                  
                        break;
                }
            }

            return (country, json.BytesConsumed);
        }
    }


    public class Country {
        private AsyncMultiCountryReader countryReader { get; set; }

        public static readonly byte[] Name_UTF8 = Encoding.UTF8.GetBytes("name");
        public string Name { get; set; }
        private AsyncUniversityReader universityReader = new AsyncUniversityReader();

        public Country(AsyncMultiCountryReader countryReader)
        {
            this.countryReader = countryReader;
        }

        public async IAsyncEnumerable<University> ReadUniversities()
        {
            await foreach (var university in universityReader.YieldReadJsonFromStreamUsingSpan(countryReader))
            {
                // Console.WriteLine("University found: " + university.Name);
                yield return university;    
            }
        }
    }
}
