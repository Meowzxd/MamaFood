using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlobStorage.Controllers
{
    public class BlobsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private CloudBlobContainer getBlobStorageInformation()
        {
            //step 1: read json
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build();

            //to get key access
            //once link, time to read the content to get the connectionstring
            CloudStorageAccount objectaccount = CloudStorageAccount.Parse(configure["ConnectionStrings:StorageConnection"]);
            CloudBlobClient blobclient = objectaccount.CreateCloudBlobClient();

            //step 2: how to create a new container in the blob storage account.
            CloudBlobContainer container = blobclient.GetContainerReference("mamafood-blobcontainer");
            return container;
        }

        public ActionResult UploadBlob()
        {
            CloudBlobContainer container = getBlobStorageInformation();
            CloudBlockBlob blob = container.GetBlockBlobReference("mamafood-blobcontainer");
            
            using (var fileStream = System.IO.File.OpenRead(@"D:\\DDAC\\Assignment\\Image1.jpg"))
            {
                blob.UploadFromStreamAsync(fileStream).Wait();
            }
            return RedirectToAction("Menu");
        }

        /*public void UploadBlob(string targetfolder, string fileName, FileStream fileStream)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(fileName) && fileStream != null)
                {
                    CloudBlobContainer container = getBlobStorageInformation();
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference(targetfolder + fileName);

                    blockBlob.UploadFromStreamAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("File Upload Failed");
            }
        }*/

        /*public string UploadBlob(string strFileName, byte[] fileData)
        {
            try
            {
                var _task = Task.Run(() => this.UploadBlobAsync(strFileName, fileData));
                _task.Wait();
                string fileUrl = _task.Result;
                return fileUrl;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        private string GenerateFileName(string fileName)
        {
            string strFileName = string.Empty;
            string[] strName = fileName.Split('.');
            strFileName = DateTime.Now.ToUniversalTime().ToString("yyyy/MM/dd") + "/" + DateTime.Now.ToUniversalTime().ToString("yyyyMMdd\\THHmmssfff") + "." + strName[strName.Length - 1];
            return strFileName;
        }

        private async Task<string> UploadBlobAsync(string strFileName, byte[] fileData)
        {
            try
            {
                CloudBlobContainer container = getBlobStorageInformation();
                string fileName = this.GenerateFileName(strFileName);

                if (fileName != null && fileData != null)
                {
                    CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(fileName);
                    await cloudBlockBlob.UploadFromByteArrayAsync(fileData, 0, fileData.Length);
                    return cloudBlockBlob.Uri.AbsoluteUri;
                }
                return "";
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }*/

        public ActionResult Menu()
        {
            CloudBlobContainer container = getBlobStorageInformation();
            //Step 2: create the empty list to store for the blobs list information 
            List<string> blobs = new List<string>();

            //step 3: get the listing record from the blob storage
            BlobResultSegment result = container.ListBlobsSegmentedAsync(null).Result;

            //step 4: to read blob listing from the storage
            foreach (IListBlobItem item in result.Results)
            {
                //step 4.1. check the type of the blob : block blob or directory or page block
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    blobs.Add(blob.Name + "#" + blob.Uri.ToString());
                }
                else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    CloudBlobDirectory blob = (CloudBlobDirectory)item;
                    blobs.Add(blob.Uri.ToString());
                }
            }
            return View(blobs);
        }

        public string DeleteBlob(string area)
        {
            CloudBlobContainer container = getBlobStorageInformation();

            //step 2: give a name for the desired blob (new blob name)
            CloudBlockBlob deletedblob = container.GetBlockBlobReference(area);

            //step 3: delete the item
            string name = deletedblob.Name;
            var result = deletedblob.DeleteIfExistsAsync().Result;

            //step 4: output message
            if (result == true)
                return "Item " + name + " is successfully deleted";
            else
                return "Item " + name + " is not able to delete";
        }
    }
}
