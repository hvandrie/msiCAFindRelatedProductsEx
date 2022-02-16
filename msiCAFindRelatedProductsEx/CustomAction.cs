using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;

namespace msiCAFindRelatedProductsEx
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult FindRelatedProductsEx(Session session)
        {
            Version productVersion = null;
            if (Version.TryParse(session["ProductVersion"], out productVersion)) session.Log(string.Format("Parsed Private Property ProductVersion:\nMajor = {0}\nMinor = {1}\nBuild = {2}\nRevision = {3}", productVersion.Major, productVersion.Minor, productVersion.Build, productVersion.Revision));

            View vUpgradeTable = session.Database.OpenView(string.Format("SELECT `UpgradeCode`,`VersionMin`,`VersionMax`,`Language`,`Attributes`,`Remove`,`ActionProperty` FROM `Upgrade` WHERE `UpgradeCode` = '{0}'", session["UpgradeCode"]));
            vUpgradeTable.Execute();
            Record rUpgradeTable = vUpgradeTable.Fetch();


            IEnumerable<ProductInstallation> relatedProducts = ProductInstallation.GetRelatedProducts(session["UpgradeCode"]);
            if (relatedProducts.Count() > 0)
            {
                session.Log(string.Format("Found {0} related product(s) installed using UpgradeCode = {1}", relatedProducts.Count(), session["UpgradeCode"]));
                foreach (ProductInstallation relatedProduct in relatedProducts)
                {
                    if (rUpgradeTable == null)
                    {
                        session.Log(string.Format("No record found in Upgrade Table"));
                        continue;
                    }
                    else
                    {
                        while (rUpgradeTable != null)
                        {
                            session.Log(string.Format("Processing record found in Upgrade Table"));

                            WiUpgrade wiUpgradeRecord = new WiUpgrade();
                            Version.TryParse(rUpgradeTable.GetString("VersionMin"), out wiUpgradeRecord.VersionMin);
                            Version.TryParse(rUpgradeTable.GetString("VersionMax"), out wiUpgradeRecord.VersionMax);
                            wiUpgradeRecord.Attributes = (wiAttributes)rUpgradeTable.GetInteger("Attributes");
                            wiUpgradeRecord.ActionProperty = rUpgradeTable.GetString("ActionProperty");
                            wiUpgradeRecord.UpgradeCode = rUpgradeTable.GetString("UpgradeCode");

                            session.Log(string.Format("Evaluating VersionMin = \"{0}\" and VersionMax = \"{1}\"", wiUpgradeRecord.VersionMin, wiUpgradeRecord.VersionMax));
                            session.Log(string.Format("Attributes = {0}", wiUpgradeRecord.Attributes.ToString()));
                            session.Log(string.Format("Attribute Flag msidbUpgradeAttributesVersionMinInclusive is now = {0}", wiUpgradeRecord.Attributes.HasFlag(wiAttributes.msidbUpgradeAttributesVersionMinInclusive).ToString()));
                            session.Log(string.Format("Attribute Flag msidbUpgradeAttributesVersionMaxInclusive is now = {0}", wiUpgradeRecord.Attributes.HasFlag(wiAttributes.msidbUpgradeAttributesVersionMaxInclusive).ToString()));
                            session.Log(string.Format("ActionProperty = {0}", wiUpgradeRecord.ActionProperty));

                            if (wiUpgradeRecord.VersionMax > relatedProduct.ProductVersion && wiUpgradeRecord.Attributes.HasFlag(wiAttributes.msidbUpgradeAttributesVersionMaxInclusive) && !(string.IsNullOrEmpty(wiUpgradeRecord.ActionProperty)))
                            {
                                session.Log(string.Format("Found eligible product to be upgraded, set ActionProperty {0} = {1}", wiUpgradeRecord.ActionProperty, relatedProduct.ProductCode));
                                session[wiUpgradeRecord.ActionProperty] = relatedProduct.ProductCode;
                            }

                            if (wiUpgradeRecord.VersionMin > relatedProduct.ProductVersion && wiUpgradeRecord.Attributes.HasFlag(wiAttributes.msidbUpgradeAttributesVersionMinInclusive) && !(string.IsNullOrEmpty(wiUpgradeRecord.ActionProperty)))
                            {
                                session.Log(string.Format("Detected Upgrade block preventing eligible product to be upgraded, removing ActionProperty {0}", wiUpgradeRecord.ActionProperty));
                                session[wiUpgradeRecord.ActionProperty] = "";
                            }


                            wiUpgradeRecord = null;
                            rUpgradeTable = vUpgradeTable.Fetch();
                        }
                    }
                }
            }
            else if (relatedProducts.Count() == 0) session.Log(string.Format("Did not find related products using UpgradeCode = {0}", session["UpgradeCode"]));


            return ActionResult.Success;
        }


        public class WiUpgrade
        {
            public Version VersionMin;
            public Version VersionMax;
            public wiAttributes Attributes;
            public string ActionProperty;
            public string UpgradeCode;
        }

        [Flags]
        public enum wiAttributes
        {
            msidbUpgradeAttributesMigrateFeatures = 1,
            msidbUpgradeAttributesOnlyDetect = 2,
            msidbUpgradeAttributesIgnoreRemoveFailure = 4,
            msidbUpgradeAttributesVersionMinInclusive = 256,
            msidbUpgradeAttributesVersionMaxInclusive = 512,
            msidbUpgradeAttributesLanguagesExclusive = 1024
        }
    }
}
