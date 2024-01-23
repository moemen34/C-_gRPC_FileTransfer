// See https://aka.ms/new-console-template for more information

using Google.Protobuf;
using Grpc.Net.Client;
using GrpcServerFileTransfer;
using GrpcServerFileTransfer.Protos;
using System;
using System.IO;
using System.IO.Enumeration;
using System.Text;

namespace GrpcClientFileTransfer
{
    class Program
    {

        private static byte[] FileBytes;
        private static string FileName;
        private static string FileExtention;



        public static async Task Main(string[] args)
        {
            SetArgs();
            //test file upload
            await UploadFileDemo(FileBytes, FileName, FileExtention);


            Console.WriteLine("////////////////////////////////");
            //test file download
            await DownloadFileDemo("hehe", ".jpg");


            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

        }


        /**
         * Method that gets file from server
         */
        private static async Task DownloadFileDemo(string FileName, string Extension)
        {
            var channel = GrpcChannel.ForAddress("http://localhost:5079");
            var client = new FileTransfer.FileTransferClient(channel);
            var response = client.DownloadFile(new MetaData { Filename = FileName, Extension = Extension });


            var ByteList = new List<byte>(); //create a List of bytes to store received bytes
            bool exists = true;
            //loop through the server's rensonses
            while (await response.ResponseStream.MoveNext(CancellationToken.None))
            {
                //if the file wasn't found/doesn't exist
                if (response.ResponseStream.Current.ResponseCase.Equals(FileResponse.ResponseOneofCase.ErrorMessage))
                {
                    //errorMessage
                    Console.WriteLine(response.ResponseStream.Current.ErrorMessage);
                    exists = false;
                    break;
                }
                else
                {   //add received bytes to byte list
                    ByteList.AddRange(response.ResponseStream.Current.ChunkData.ToByteArray());
                }
            }

            if (exists)
            {
                byte[] x = ByteList.ToArray();

                //TEST, Create file in path
                using var writer = new BinaryWriter(File.OpenWrite(@"C:/Users/someone/AlsoCreated.xyz"));
                writer.Write(x);
            }
        }

        /**
         * Metheod that Sends a file to the server
         */
        private static async Task UploadFileDemo(byte[] bytes, string fName, string fExtention) 
        {
           
            var channel = GrpcChannel.ForAddress("http://localhost:5079");
            var client = new FileTransfer.FileTransferClient(channel);
            var stream = client.UploadFile();


            //construct the MetaData message
            MetaData md = new MetaData();
            md.Filename = FileName;
            md.Extension = FileExtention;

            //send the MetaData
            await stream.RequestStream.WriteAsync(new UploadFileRequest { Metadata = md });

            //create a MemoryStream to facilitate sending the bytes
            MemoryStream ms = new MemoryStream(bytes);   


            try
            {            
                int length = (int)ms.Length; // get file length
                int buffSize = 4000000;      // set buffer size to 4MB (ie. Max)
                int sum = 0;                 // keep track of sent bytes


                int BytesToSend = (int)ms.Length;  //get the number of bytes to send

                //if the number of bytes to send is less than 4MB
                if (BytesToSend < buffSize)
                {
                    buffSize = BytesToSend;// set the buffer's size to the number of bytes to send
                } 


                while (BytesToSend > 0) // Loop untill all the bytes are sent
                {
                    byte[] buffer = new byte[buffSize];   //create buffer for sending the bytes
                    int n = ms.Read(buffer, 0, buffSize); //read into the buffer 

                    //send the chunks/buffer to the server
                    await stream.RequestStream.WriteAsync(new UploadFileRequest { ChunkData = ByteString.CopyFrom(buffer) });

                    if (n == 0)
                    {
                        break; //all bytes are sent
                    }

                    BytesToSend -= n; //update the number of bytes to send
                    sum += n;         //update the sum

                    if (BytesToSend<buffSize)
                    {
                        buffSize = BytesToSend; //decrese the size of the buffer if needed when the end is reached
                    }

                }

                await stream.RequestStream.CompleteAsync();           //let the server know we are done
                StringResponse response = await stream.ResponseAsync; //get the response from the server

                Console.WriteLine("Bytes to send: "+BytesToSend +" / Bytes sent: "+ sum);

                Console.WriteLine("Server responce: "+response.Message);
            }
            finally
            {
                ms.Close();
            }  
        }


        private static void SetArgs()
        {
            string path = @"C:/Users/someone/ToSend.xyz";

            string FileNameAndExtention = Path.GetFileName(path);

            string[] FileNE = FileNameAndExtention.Split('.');
            if (FileNE.Length != 2)
            {
                throw new Exception("INVALID FILE NAME!!");
            }

            //set the global variables
            //arguments for upload
            FileBytes = File.ReadAllBytes(path);
            FileName = FileNE[0];
            FileExtention = FileNE[1];

        }

    }
}




