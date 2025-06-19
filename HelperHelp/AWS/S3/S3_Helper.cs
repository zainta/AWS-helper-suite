using System;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace HelperHelp.AWS.S3
{
    /// <summary>
    /// Provides various static methods for usage with the AWS S3 service
    /// </summary>
    public static class S3_Helper
    {
        /// <summary>
        /// Creates and returns an Amazon S3 client
        /// </summary>
        /// <param name="region">The region to perform the operation in.  Defaults to US-East-1</param>
        /// <returns></returns>
        public static IAmazonS3 getClient(Amazon.RegionEndpoint region)
        {
            if (region == null)
            {
                region = Amazon.RegionEndpoint.USEast1;
            }
            var client = new AmazonS3Client(region);

            return client;
        }

        /// <summary>
        /// Attempts to upload a file to the given S3 bucket with the given key file (credentials)
        /// </summary>
        /// <param name="client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The name of the S3 bucket</param>
        /// <param name="keyName">The S3 key to use for the file</param>
        /// <param name="uploadFile">The complete path of the file to upload</param>
        /// <exception cref="Exception">Thrown if an error is encountered</exception>
        public static async Task<bool> UploadFile(IAmazonS3 client, string bucketName, string keyName, string uploadFile)
        {
            bool result = false;

            try
            {
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    FilePath = uploadFile,
                    ContentType = "text/plain"
                };

                PutObjectResponse response = await client.PutObjectAsync(putRequest);
                result = true;
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Check the provided AWS Credentials.");
                }
                else
                {
                    throw new Exception("Error occurred: " + amazonS3Exception.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Deletes the given bucket and its contents
        /// </summary>
        /// <param name="client">The client to delete through</param>
        /// <param name="bucketName">The name of the bucket to delete</param>
        public static async Task<bool> DeleteBucket(IAmazonS3 client, string bucketName)
        {
            bool result = false;

            try
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(client, bucketName);
                result = true;
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId")
                    ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Check the provided AWS Credentials.");
                }
                else
                {
                    throw new Exception("Error occurred: " + amazonS3Exception.Message);
                }
            }

            return result;
        }

        /// <summary>
        /// Shows how to create a new Amazon S3 bucket.
        /// </summary>
        /// <param name="client">An initialized Amazon S3 client object.</param>
        /// <param name="bucketName">The name of the bucket to create.</param>
        /// <returns>A boolean value representing the success or failure of
        /// the bucket creation process.</returns>
        public static async Task<bool> CreateBucketAsync(IAmazonS3 client, string bucketName)
        {
            try
            {
                var request = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true,
                };

                var response = await client.PutBucketAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error creating bucket: '{ex.Message}'");
                return false;
            }
        }

        /// <summary>
        /// Checks to see if an S3 bucket exists, and can create it if not.
        /// </summary>
        /// <param name="client">The aws Amazon client instance to use</param>
        /// <param name="bucket">The bucket to check for</param>
        /// <param name="create">Whether or not to create if it doesn't exist</param>
        /// <param name="token">A cancellation token if it attempts to create</param>
        /// <returns></returns>
        public static async Task<bool> BucketExistsAsync(IAmazonS3 client, string bucket, bool create, CancellationToken token)
        {
            if (string.IsNullOrEmpty(bucket)) return false;

            if (await AmazonS3Util.DoesS3BucketExistV2Async(client, bucket)) return true;
            if (create) await client.PutBucketAsync(new PutBucketRequest { BucketName = bucket, UseClientRegion = true }, token);

            return false;
        }
    }
}