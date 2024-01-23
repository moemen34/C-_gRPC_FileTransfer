



using Google.Protobuf;
using Grpc.Core;
using GrpcServerFileTransfer.Protos;
using System;
using System.Collections;
using System.IO;

namespace GrpcServerFileTransfer.Services
{
    public class FileTransferService : FileTransfer.FileTransferBase
    {
        private readonly ILogger<FileTransferService> _logger;

        public FileTransferService(ILogger<FileTransferService> logger)
        {
            _logger = logger;
        }


        private static byte[] testBytes; //byte array to test sending fies to client


        //server gets upload request. receives the data ie client wants to upload to server
        public override async Task<StringResponse> UploadFile(IAsyncStreamReader<UploadFileRequest> requestStream, ServerCallContext context)
        {

            int i = 0; //to check for the first message
            var ByteList = new List<byte>(); //create a List of bytes to store received bytes

            while (await requestStream.MoveNext()) //loop while receiving data chunks
            {
                if (i == 0) // if this is the fist message received
                {
                    //verify that the message contains MetaData
                    if (requestStream.Current.RequestCase.Equals(UploadFileRequest.RequestOneofCase.Metadata))
                    {
                        string FileName = requestStream.Current.Metadata.Filename;  // get the file's name 
                        string Extension = requestStream.Current.Metadata.Extension;// get the file's extention

                        Console.WriteLine($"File Name: {FileName} Extension: {Extension}");
                    }
                    else //throw an exception if the message didn't have MetaData
                    {
                        throw new Exception("Meta Data Expexted!!");
                    }
                    i++;

                }else if (requestStream.Current.RequestCase.Equals(UploadFileRequest.RequestOneofCase.ChunkData))
                { //the current message has file chunks

                    //add the bytes received to the List of Bytes
                    ByteList.AddRange(requestStream.Current.ChunkData.ToByteArray());
                }
                else // throw an exception if this isn't the first message and isn't a file chunk
                {
                    throw new Exception("Bytes of Data Expexted!!");
                }
            }

            byte[] x = ByteList.ToArray(); // Byte array to store in database

            //TESTING THE DOWNLOAD
            testBytes = x;

            //TESTING this Upload

            Console.WriteLine("end x: " + x.Length);

            using var writer = new BinaryWriter(File.OpenWrite(@"C:/Users/someone/Created.xyz"));
            writer.Write(x);

            return new StringResponse { Message = "success!!!!!!!!!!!"};
        }


        

        public override async Task DownloadFile(MetaData request, IServerStreamWriter<FileResponse> responseStream, ServerCallContext context)
        {

            string FileName = request.Filename;  //get file name 
            string Extension = request.Extension;//get file extention
            
            byte[] x = getByteArray(FileName, Extension); //get file as byte array

            if (x == null)
            {
                await responseStream.WriteAsync(new FileResponse { ErrorMessage = "FILE DOEN't EXIST!!!"});
            }
            else
            {
                int BytesToSend = x.Length;
                int buffSize = 4000000;      // set buffer size to 4MB (ie. Max)
                int sum = 0;                 // keep track of sent bytes


                //if the number of bytes to send is less than 4MB
                if (BytesToSend < buffSize)
                {
                    buffSize = BytesToSend;// set the buffer's size to the number of bytes to send
                }

                //create a memory stream for simplicity
                MemoryStream ms = new MemoryStream(x);

                while (BytesToSend > 0) // Loop untill all the bytes are sent
                {
                    byte[] buffer = new byte[buffSize];   //create buffer for sending the bytes
                    int n = ms.Read(buffer, 0, buffSize); //read into the buffer 

                    //send the chunks/buffer to the server
                    await responseStream.WriteAsync(new FileResponse { ChunkData = ByteString.CopyFrom(buffer) });

                    Console.WriteLine($"DATA SENT!!!!!!!!: {n} bytes sent");

                    if (n == 0)
                    {
                        break; //all bytes are sent
                    }

                    BytesToSend -= n; //update the number of bytes to send
                    sum += n;         //update the sum

                    if (BytesToSend < buffSize)
                    {
                        buffSize = BytesToSend; //decrese the size of the buffer if needed when the end is reached
                    }

                }


            }
        }



        /**
         * method that returns file as bytes, this could be changed 
         * to DataBase or directory search...
         */
        public static byte[] getByteArray(string FileName, String FileExtension)
        {
            if (testBytes != null)
                return testBytes;
            else
                return null;
        }



    }
}
