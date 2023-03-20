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
        private readonly AsyncMultiCountryReader countryReader;

        public AsyncUniversityReader(AsyncMultiCountryReader countryReader)
        {
            this.countryReader = countryReader;
        }
        //public async IAsyncEnumerable<University> YieldReadJsonFromStreamUsingSpan(AsyncMultiCountryReader countryReader)
        //{  
        //    University university;

        //    while (true)
        //    {
        //        await countryReader.ReadStream();

        //        (university, countryReader.bytesConsumed) = ReadUniversity(countryReader.buffer.AsSpan(0, countryReader.currentDataSize), countryReader.isFinalBlock, ref countryReader.state, countryReader);

        //        countryReader.UpdateBufferPostRead();


        //        if (countryReader.isFinalBlock || university == null)
        //        {
        //            break;
        //        }

        //        yield return university;
        //    }
        //}


        public (University university, long bytesConsumed) ReadUniversity(ReadOnlySpan<byte> dataUtf8, bool isFinalBlock, ref JsonReaderState state, AsyncMultiCountryReader countryReader)
        {
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock, state);
            University? univerisity = new University();

            while (json.Read() && json.TokenType != JsonTokenType.EndObject)
            {
                switch (json.TokenType)
                {
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
                        else if (json.ValueSpan.SequenceEqual(University.Sports_UTF8))
                        {
                            // Process domains array
                            univerisity.Sports = new List<Sport>();
                            Sport sport = null;
                            while (json.Read() && json.TokenType != JsonTokenType.EndArray)
                            {                                
                                if (json.TokenType == JsonTokenType.StartObject)
                                {
                                    sport = new Sport();
                                    univerisity.Sports.Add(sport);
                                }
                                else if (json.TokenType == JsonTokenType.PropertyName)
                                {
                                    if (json.ValueSpan.SequenceEqual(Sport.Location_UTF8))
                                    {
                                        json.Read();
                                        sport.Location = json.GetString();
                                    }
                                    if (json.ValueSpan.SequenceEqual(Sport.Type_UTF8))
                                    {
                                        json.Read();
                                        sport.Type = json.GetString();
                                    }
                                }
                            }
                        }

                        break;

                }
            }

            if (json.TokenType == JsonTokenType.EndArray)
            {
                Console.WriteLine("End of universities");
            }


            state = json.CurrentState;
            return (univerisity, json.BytesConsumed);
        }
    }
}