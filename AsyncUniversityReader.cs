using JsonReader;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReaderSample
{
    class AsyncUniversityReader
    {
        public async Task RunSample()
        {
            await YieldReturnSample(@"http://universities.hipolabs.com/search?country=United%20States");
        }



        private async Task YieldReturnSample(string url)
        {
            using (var client = new HttpClient())
            {
                using (Stream stream = await client.GetStreamAsync(url))
                {
                    await foreach (var university in YieldReadJsonFromStreamUsingSpan(stream))
                    {
                        Console.WriteLine("University found: " + university.Name);
                    }
                }
            }
        }

        public async IAsyncEnumerable<University> YieldReadJsonFromStreamUsingSpan(Stream stream)
        {
            // Assumes all JSON strings in the payload are small (say < 500 bytes)
            var buffer = new byte[1_024];           

            JsonReaderState state = default;
            int leftOver = 0;          

            University university;

            while (true)
            {
                // The Memory<byte> ReadAsync overload returns ValueTask which is allocation-free
                // if the operation completes synchronously
                int dataLength = await stream.ReadAsync(buffer.AsMemory(leftOver, buffer.Length - leftOver));
                int dataSize = dataLength + leftOver;
                bool isFinalBlock = dataSize == 0;
                long bytesConsumed = 0;

                (university, bytesConsumed) = ReadUniversity(buffer.AsSpan(0, dataSize), isFinalBlock, ref state);

                // Based on your scenario and input data, you may need to grow your buffer here
                // It's possible that leftOver == dataSize (if a JSON token is too large)
                // so you need to resize and read more than 1_024 bytes.
                leftOver = dataSize - (int)bytesConsumed;
                if (leftOver != 0)
                {
                    buffer.AsSpan(dataSize - leftOver, leftOver).CopyTo(buffer);
                }

                if (isFinalBlock || university == null)
                {
                    break;
                }
                yield return university;
            }           
        }

        public async IAsyncEnumerable<University> YieldReadJsonFromStreamUsingSpan(AsyncMultiCountryReader countryReader)
        {  

            University university;

            while (true)
            {
                // The Memory<byte> ReadAsync overload returns ValueTask which is allocation-free
                // if the operation completes synchronously
                int dataLength = await countryReader.stream.ReadAsync(countryReader.buffer.AsMemory(countryReader.leftOver, countryReader.buffer.Length - countryReader.leftOver));
                int dataSize = dataLength + countryReader.leftOver;
                bool isFinalBlock = dataSize == 0;
               

                (university, countryReader.bytesConsumed) = ReadUniversity(countryReader.buffer.AsSpan(0, dataSize), isFinalBlock, ref countryReader.state);

                // Based on your scenario and input data, you may need to grow your buffer here
                // It's possible that leftOver == dataSize (if a JSON token is too large)
                // so you need to resize and read more than 1_024 bytes.
                countryReader.leftOver = dataSize - (int)countryReader.bytesConsumed;
                if (countryReader.leftOver != 0)
                {
                    countryReader.buffer.AsSpan(dataSize - countryReader.leftOver, countryReader.leftOver).CopyTo(countryReader.buffer);
                }

                if (isFinalBlock || university == null)
                {
                    break;
                }
                yield return university;
            }
        }


        public (University university, long bytesConsumed) ReadUniversity(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, ref JsonReaderState state)
        {         
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock, state);
            University univerisity = null;

            while (json.Read() && json.TokenType != JsonTokenType.EndObject && json.TokenType != JsonTokenType.EndArray)
            {
                JsonTokenType tokenType = json.TokenType;

                switch (tokenType)
                {
                    case JsonTokenType.StartObject:
                        univerisity = new University();
                        break;
                    case JsonTokenType.PropertyName:
                        if (json.ValueSpan.SequenceEqual(University.StateProvince_UTF8))
                        {                           
                            json.Read();
                            
                            univerisity.StateProvince = json.GetString();
                        }
                        else if (json.ValueSpan.SequenceEqual(University.Country_UTF8))
                        {

                            json.Read();
                            univerisity.Country = json.GetString();
                        }
                        else if (json.ValueSpan.SequenceEqual(University.Name_UTF8))
                        {
                            json.Read();
                            univerisity.Name = json.GetString();
                        }
                        else if (json.ValueSpan.SequenceEqual(University.AlphaTwoCode_UTF8))
                        {
                            json.Read();
                            univerisity.AlphaTwoCode = json.GetString();
                        }
                        else if (json.ValueSpan.SequenceEqual(University.Domains_UTF8))
                        {
                            // Process domains array
                            univerisity.Domains = new List<string>();
                            while (json.Read() && json.TokenType != JsonTokenType.EndArray)
                            {
                                if (json.TokenType == JsonTokenType.String)
                                {
                                    univerisity.Domains.Add(json.GetString());
                                }
                            }
                        }
                        else if (json.ValueSpan.SequenceEqual(University.WebPages_UTF8))
                        {
                            // Process web pages array
                            univerisity.WebPages = new List<string>();
                            while (json.Read() && json.TokenType != JsonTokenType.EndArray)
                            {
                                if (json.TokenType == JsonTokenType.String)
                                {
                                    univerisity.WebPages.Add(json.GetString());
                                }
                            }
                        }                        

                        break;

                }
            }

            if(json.TokenType == JsonTokenType.EndArray)
            {
                Console.WriteLine("End of universities");               
            }

            state = json.CurrentState;
            return (univerisity, json.BytesConsumed);
        }
    }
}