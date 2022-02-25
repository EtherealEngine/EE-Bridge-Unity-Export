using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace XREngine.XREngineProject
{
    public class ECommerceProduct : RPComponent
    {
        public enum Provider
        {
            SHOPIFY,
            WOO_WOO_COMMERCE
        }

        private readonly string[] providerStrs = new[]
        {
            "shopify",
            "woocommerce"
        };

        public Provider provider;

        public string domain;

        private string ToStr(Provider _provider) { return providerStrs[(int)_provider]; }

        public override string Type => base.Type + ".e-commerce-product";

        public override JProperty Serialized => new JProperty("extras", new JObject
        (
            new JProperty(Type + ".provider", ToStr(provider)),
            new JProperty(Type + ".domain", domain),
            new JProperty("xrengine.entity", transform.name)
        ));
    }
}

