using System;
using System.Collections.Generic;
using System.Linq;
using Service.AssetsDictionary.Client;
using Service.AssetsDictionary.Domain.Models;

namespace Service.Bitgo.DepositDetector.Services
{
    public interface IAssetMapper
    {
        IAsset GetAssetForBitGoAsync(string coin, string walletId);
        double ConvertAmountFromBitgo(string coin, long amount);
    }

    public class AssetMapper : IAssetMapper
    {
        public Dictionary<string, double> AccuracyByAsset = new Dictionary<string, double>()
        {
            { "algo", Math.Pow(10, 0) },
            { "bch", Math.Pow(10, 0) },
            { "btc", Math.Pow(10, 0) },
            { "dash", Math.Pow(10, 0) },
            { "eos", Math.Pow(10, 0) },
            { "eth", Math.Pow(10, 0) },
            { "hbar", Math.Pow(10, 0) },
            { "ltc", Math.Pow(10, 0) },
            { "trx", Math.Pow(10, 0) },
            { "xlm", Math.Pow(10, 7) },
            { "xrp", Math.Pow(10, 0) },
            { "zec", Math.Pow(10, 0) },

            { "talgo", Math.Pow(10, 0) },
            { "tbch", Math.Pow(10, 0) },
            { "tbtc", Math.Pow(10, 0) },
            { "tdash", Math.Pow(10, 0) },
            { "teos", Math.Pow(10, 0) },
            { "teth", Math.Pow(10, 0) },
            { "thbar", Math.Pow(10, 0) },
            { "tltc", Math.Pow(10, 0) },
            { "ttrx", Math.Pow(10, 0) },
            { "txlm", Math.Pow(10, 7) },
            { "txrp", Math.Pow(10, 0) },
            { "tzec", Math.Pow(10, 0) },
        };

        private readonly IAssetsDictionaryClient _assetsDictionaryClient;

        public AssetMapper(IAssetsDictionaryClient assetsDictionaryClient)
        {
            _assetsDictionaryClient = assetsDictionaryClient;
        }

        public IAsset GetAssetForBitGoAsync(string coin, string walletId)
        {
            var symbol = SymbolFromBitgo(coin);

            var asset = _assetsDictionaryClient.GetAllAssets().FirstOrDefault(e => e.BrokerId == Program.Settings.DefaultBrokerId && e.Symbol == symbol);

            return asset;
        }

        public double ConvertAmountFromBitgo(string coin, long amount)
        {
            var koef = AccuracyByAsset[coin];
            return ((double) amount) / koef;
        }

        private string SymbolFromBitgo(string coin)
        {
            switch (coin)
            {
                case "algo": return "ALGO";
                case "bch": return "BCH";
                case "btc": return "BTC";
                case "dash": return "DASH";
                case "eos": return "EOS";
                case "eth": return "ETH";
                case "hbar": return "HBAR";
                case "ltc": return "LTC";
                case "trx": return "TRX";
                case "xlm": return "XLM";
                case "xrp": return "XRP";
                case "zec": return "ZEC";
            }

            switch (coin)
            {
                case "talgo": return "ALGO";
                case "tbch": return "BCH";
                case "tbtc": return "BTC";
                case "tdash": return "DASH";
                case "teos": return "EOS";
                case "teth": return "ETH";
                case "thbar": return "HBAR";
                case "tltc": return "LTC";
                case "ttrx": return "TRX";
                case "txlm": return "XLM";
                case "txrp": return "XRP";
                case "tzec": return "ZEC";
            }

            return string.Empty;
        }
    }
}