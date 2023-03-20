using ReaderSample;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonReader
{
    public class AsyncMultiCountryReader
    {
        public byte[] buffer = new byte[1_024];
        public int leftOver = 0;
        public long bytesConsumed = 0;
        public Stream stream;
        public JsonReaderState state = default;
        public int currentDataSize = 0;
        public bool isFinalBlock = false;

        public AsyncMultiCountryReader()
        {
            stream = new FileStream("multi_country_universities_slim.json", FileMode.Open);
            //stream = new FileStream("multi_country_universities.json", FileMode.Open);
        }

        public bool EnsureBytesForRead(ref Utf8JsonReader reader)
        {
            //while (true)
            //{
            //    try
            //    {
            //        while (!reader.Read() && stream.Position < stream.Length)
            //        {
            //            // Not enough of the JSON is in the buffer to complete a read.
            //            GetMoreBytesFromStream(stream, buffer, ref reader);
            //        }
            //        return true;
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("error", ex);
            //        GetMoreBytesFromStream(stream, buffer, ref reader);
            //    }
            //}

            while (!reader.Read() && stream.Position < stream.Length)
            {
                // Not enough of the JSON is in the buffer to complete a read.
                GetMoreBytesFromStream(stream, buffer, ref reader);
            }
            return true;
        }

        private void GetMoreBytesFromStream(Stream stream, byte[] buffer, ref Utf8JsonReader reader)
        {
            int bytesRead;
            if (reader.BytesConsumed < buffer.Length)
            {
                ReadOnlySpan<byte> leftover = buffer.AsSpan((int)reader.BytesConsumed);

                if (leftover.Length == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                    Console.WriteLine($"Increased buffer size to {buffer.Length}");
                }

                leftover.CopyTo(buffer);
                bytesRead = stream.Read(buffer.AsSpan(leftover.Length));
            }
            else
            {
                bytesRead = stream.Read(buffer);
            }
            //Console.WriteLine("More bytes retrieved");
            // Console.WriteLine($"Updated String in buffer is: {Encoding.UTF8.GetString(buffer)}");
            reader = new Utf8JsonReader(buffer, isFinalBlock: bytesRead == 0, reader.CurrentState);
        }

        public async Task<object> ReadObject()
        {
            await ReadStream();

            UpdateBufferPostRead();

            return null;
        }


        public async IAsyncEnumerable<Country> ReadCountries()
        {
            Country country;

            while (true)
            {

                //Console.WriteLine($"buffer text {Encoding.UTF8.GetString(buffer)}");

                await ReadStream();
                (country, bytesConsumed) = ReadCountry(buffer.AsSpan(0, currentDataSize), isFinalBlock, ref state);

                UpdateBufferPostRead();

                if (isFinalBlock || country == null)
                {
                    break;
                }


                //try
                //{
                //    await ReadStream();
  
                //    (country, bytesConsumed) = ReadCountry(buffer.AsSpan(0, currentDataSize), isFinalBlock, ref state);

                //    UpdateBufferPostRead();

                //    if (isFinalBlock || country == null)
                //    {
                //        break;
                //    }

                  
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine($"buffer text {Encoding.UTF8.GetString(buffer)}");
                //    bytesConsumed++;
                //    UpdateBufferPostRead();
                //    continue;
                //    ////throw;
                //}

                yield return country;

            }
        }

        public async Task ReadStream()
        {
            int dataLength = await stream.ReadAsync(buffer.AsMemory(leftOver, buffer.Length - leftOver));
            currentDataSize = dataLength + leftOver;
            isFinalBlock = currentDataSize == 0;
        }

         public void UpdateBufferPostRead()
        {
            leftOver = currentDataSize - (int)bytesConsumed;
            if (leftOver != 0)
            {
                buffer.AsSpan(currentDataSize - leftOver, leftOver).CopyTo(buffer);
            }            
        }


        public (Country country, long bytesConsumed) ReadCountry(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, ref JsonReaderState state)
        {
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock, state);
            Country? country = null;

            var needsRead = true;
           
            while (needsRead && json.Read())
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

            // Forward reader to the universities array
            needsRead = true;
            while (needsRead && json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                switch (tokenType)
                {                   
                    case JsonTokenType.PropertyName:
                        if (json.ValueSpan.SequenceEqual(Country.Universities_UTF8))
                        {                          
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
        public static readonly byte[] Universities_UTF8 = Encoding.UTF8.GetBytes("universities");
        public string Name { get; set; }
        // private AsyncUniversityReader universityReader = new AsyncUniversityReader();

        public Country(AsyncMultiCountryReader countryReader)
        {
            this.countryReader = countryReader;
        }

        public async IAsyncEnumerable<University> ReadUniversities()
        {
            //await foreach (var university in universityReader.YieldReadJsonFromStreamUsingSpan(countryReader))
            //{
            //    // Console.WriteLine("University found: " + university.Name);
            //    yield return university;
            //}

            University university;
            var reader = new AsyncUniversityReader(this.countryReader);

            while (true)
            {
                await countryReader.ReadStream();

                //(university, countryReader.bytesConsumed) = reader.ReadUniversity(countryReader.buffer.AsSpan(0, countryReader.currentDataSize), countryReader.isFinalBlock, ref countryReader.state, countryReader);
               
                (university, countryReader.bytesConsumed) = ReadUniversitiesArrayItems(countryReader.buffer.AsSpan(0, countryReader.currentDataSize), countryReader.isFinalBlock, ref countryReader.state, countryReader);

                countryReader.UpdateBufferPostRead();


                if (countryReader.isFinalBlock || university == null)
                {
                    break;
                }

                yield return university;
            }
        }

        public (University university, long bytesConsumed) ReadUniversitiesArrayItems(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, ref JsonReaderState state, AsyncMultiCountryReader countryReader)
        {
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock, state);
            var reader = new AsyncUniversityReader(this.countryReader);

            if (json.Read() && json.TokenType != JsonTokenType.EndArray)
            {
                return reader.ReadUniversity(countryReader.buffer.AsSpan(0, countryReader.currentDataSize), countryReader.isFinalBlock, ref countryReader.state, countryReader);
            }
            else
            {
                Console.WriteLine("End of universities");                
            }

            return (null, json.BytesConsumed);
        }
    }
}
