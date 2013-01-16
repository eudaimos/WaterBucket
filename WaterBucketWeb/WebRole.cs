using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace WaterBucketWeb
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            int bucketSizeMax;
            try
            {
                string bsizeMax = RoleEnvironment.GetConfigurationSettingValue("Bucket.Size.Max");
            }
            catch (RoleEnvironmentException rex)
            {

            }

            return base.OnStart();
        }
    }
}
