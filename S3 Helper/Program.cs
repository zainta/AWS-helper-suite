using Amazon;
using HelperHelp;
using HelperHelp.AWS.S3;
using ReddWare.IO.Parameters;
using System.IO.Compression;

const string TargetRegion = "region";
const string CheckS3BucketExists = "exists";
const string CreateS3Bucket = "create";
const string DeleteS3Bucket = "delete";
const string EnsureS3Bucket = "ensure";
const string BundleToBucket = "bundle";
const string BundleSourcePaths = "bundles";

var ph = new ParameterHandler();
ph.AddRules(
        new ParameterRuleOption(TargetRegion, false, false, null, "-"),
        new ParameterRuleOption(CheckS3BucketExists, false, false, null, "-"),
        new ParameterRuleOption(CreateS3Bucket, false, false, null, "-"),
        new ParameterRuleOption(DeleteS3Bucket, false, false, null, "-"),
        new ParameterRuleOption(EnsureS3Bucket, false, false, null, "-"),
        new ParameterRuleOption(BundleToBucket, false, false, null, "-"),
        new ParameterRuleOption(BundleSourcePaths, true, false, null, "-"),
        new ParameterRuleFlag(new FlagDefinition[]
        {
            new FlagDefinition('?', false, false),
            new FlagDefinition('s', false, false)
        },
        "-"
    ));
ph.Comb(args);

bool beVocal = !ph.GetFlag("s");

if (ph.GetFlag("?") || args.Length == 0)
{
    HH.Spout("Help requests", beVocal);
}

string exists = ph.GetParam(CheckS3BucketExists);
string create = ph.GetParam(CreateS3Bucket);
string delete = ph.GetParam(DeleteS3Bucket);
string ensure = ph.GetParam(EnsureS3Bucket);
string bundleBucket = ph.GetParam(BundleToBucket);
string[] bundles = ph.GetAllParam(BundleSourcePaths);
int result = (int)ExitCodes.Success;

var Acceptable = (int resultValue) =>
{
    return resultValue == (int)ExitCodes.Success;
};

if ((!String.IsNullOrWhiteSpace(bundleBucket) && bundles.Length == 0))
{
    Console.WriteLine("To bundle, both bundle and bundles must be supplied");
    Environment.Exit(result);
}

if (!String.IsNullOrWhiteSpace(exists) || !String.IsNullOrWhiteSpace(create)
    || !String.IsNullOrWhiteSpace(delete) || !String.IsNullOrWhiteSpace(ensure)
    || (!String.IsNullOrWhiteSpace(bundleBucket) && bundles.Length > 0))
{
    string region = ph.GetParam(TargetRegion);
    if (String.IsNullOrWhiteSpace(region))
    {
        HH.Spout("Region is required", beVocal);
        result = (int)ExitCodes.Failure;
    }
    else
    {
        var regionInstance = HH.GetRegionEndpoint(region);
        if (regionInstance != null)
        {
            using (var client = S3_Helper.getClient((RegionEndpoint)regionInstance))
            {
                if (!String.IsNullOrWhiteSpace(ensure) && Acceptable(result))
                {
                    CancellationToken cancellationToken = new CancellationToken();
                    if (await S3_Helper.BucketExistsAsync(client, ensure, true, cancellationToken))
                    {
                        HH.Spout("Found it", beVocal);
                        result = (int)ExitCodes.Success;
                    }
                    else
                    {
                        HH.Spout("Ensured it", beVocal);
                        result = (int)ExitCodes.Success;
                    }
                }

                if (!String.IsNullOrWhiteSpace(exists) && Acceptable(result))
                {
                    CancellationToken cancellationToken = new CancellationToken();
                    if (await S3_Helper.BucketExistsAsync(client, exists, false, cancellationToken))
                    {
                        HH.Spout("Found it", beVocal);
                        result = (int)ExitCodes.Success;
                    }
                    else
                    {
                        HH.Spout("Didn't find it", beVocal);
                        result = (int)ExitCodes.Failure;
                    }
                }

                if (!String.IsNullOrWhiteSpace(create) && Acceptable(result))
                {
                    if (await S3_Helper.CreateBucketAsync(client, create))
                    {
                        HH.Spout("Done", beVocal);
                        result = (int)ExitCodes.Success;
                    }
                    else
                    {
                        HH.Spout("Failed", beVocal);
                        result = (int)ExitCodes.Failure;
                    }
                }

                if (!String.IsNullOrWhiteSpace(bundleBucket) && bundles.Length > 0 && Acceptable(result))
                {
                    var zips = new List<Tuple<string, string>>();

                    HH.Spout("Starting...", beVocal);
                    await Parallel.ForEachAsync(bundles, async (path, cancellationToken) =>
                    {
                        if (Directory.Exists(path)) {
                            var dir = new DirectoryInfo(path);
                            var name = dir.Name + "_bundle.zip";
                            HH.Spout(name, beVocal, false);
                            if (File.Exists(name))
                            {
                                File.Delete(name);
                            }
                            ZipFile.CreateFromDirectory(path, name);
                            zips.Add(new Tuple<string, string>(name, name));
                            HH.Spout("Done", beVocal);
                        }
                    });

                    HH.Spout("Uploading content...", beVocal);
                    foreach (var zip in zips)
                    {
                        var awaiter = S3_Helper.UploadFile(client, bundleBucket, zip.Item2, zip.Item1).GetAwaiter();
                        HH.Spout("Adding '" + zip.Item2 + "'", beVocal, false);
                        while (!awaiter.IsCompleted)
                        {
                            HH.Spout(".", beVocal, false);
                            await Task.Delay(1000);
                        }
                        HH.Spout("Done", beVocal);
                    }
                    HH.Spout("Finished uploads.", beVocal);

                    HH.Spout("Cleaning up", beVocal, false);
                    for (int i = 0; i < zips.Count; i++)
                    {
                        File.Delete(zips[i].Item1);
                        HH.Spout(".", beVocal, false);
                    }
                    HH.Spout("Done", beVocal);
                    HH.Spout("Bundle completed.", beVocal);
                }

                if (!String.IsNullOrWhiteSpace(delete) && Acceptable(result))
                {
                    CancellationToken cancellationToken = new CancellationToken();
                    if (await S3_Helper.BucketExistsAsync(client, delete, false, cancellationToken))
                    {
                        if (await S3_Helper.DeleteBucket(client, delete))
                        {
                            HH.Spout("Done", beVocal);
                            result = (int)ExitCodes.Success;
                        }
                        else
                        {
                            HH.Spout("Failed", beVocal);
                            result = (int)ExitCodes.Failure;
                        }
                    }
                    else
                    {
                        HH.Spout("Didn't find it", beVocal);
                        result = (int)ExitCodes.Failure;
                    }
                }
            }
        } else
        {
            HH.Spout("Unknown Region", beVocal, false);
            result = (int)ExitCodes.Failure;
        }
    }
}

Environment.Exit(result);