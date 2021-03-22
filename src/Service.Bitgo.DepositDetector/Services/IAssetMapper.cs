using System;
using System.Collections.Generic;
using System.Linq;
using MyJetWallet.Domain.Assets;
using Service.AssetsDictionary.Client;
using Service.AssetsDictionary.Domain.Models;
using Service.Bitgo.DepositDetector.Settings;

namespace Service.Bitgo.DepositDetector.Services
{
    public interface IAssetMapper
    {
        IAsset GetAssetForBitGoAsync(string coin, string walletId);
        (string, string) GetAssetToBitGoAsync(IAssetIdentity assetId);
        double ConvertAmountFromBitgo(string coin, long amount);
    }

    public class AssetMapper : IAssetMapper
    {
        public Dictionary<string, double> AccuracyByAsset = new Dictionary<string, double>()
        {
            { "algo", Math.Pow(10, 6) },
            { "bch", Math.Pow(10, 8) },
            { "btc", Math.Pow(10, 8) },
            { "dash", Math.Pow(10, 6) },
            { "eos", Math.Pow(10, 4) },
            //{ "eth", Math.Pow(10, 18) },
            //{ "hbar", Math.Pow(10, 0) },
            { "ltc", Math.Pow(10, 8) },
            { "trx", Math.Pow(10, 6) },
            { "xlm", Math.Pow(10, 7) },
            { "xrp", Math.Pow(10, 6) },
            { "zec", Math.Pow(10, 8) },

            { "talgo", Math.Pow(10, 6) },
            { "tbch", Math.Pow(10, 8) },
            { "tbtc", Math.Pow(10, 8) },
            { "tdash", Math.Pow(10, 6) },
            { "teos", Math.Pow(10, 4) },
            //{ "teth", Math.Pow(10, 18) },
            //{ "thbar", Math.Pow(10, 0) },
            { "tltc", Math.Pow(10, 8) },
            { "ttrx", Math.Pow(10, 6) },
            { "txlm", Math.Pow(10, 7) },
            { "txrp", Math.Pow(10, 6) },
            { "tzec", Math.Pow(10, 8) },
        };

        private readonly IAssetsDictionaryClient _assetsDictionaryClient;

        public AssetMapper(IAssetsDictionaryClient assetsDictionaryClient, SettingsModel settingsModel)
        {
            _assetsDictionaryClient = assetsDictionaryClient;
        }

        public IAsset GetAssetForBitGoAsync(string coin, string walletId)
        {
            var symbol = SymbolFromBitgo(coin);

            var asset = _assetsDictionaryClient.GetAllAssets().FirstOrDefault(e => e.BrokerId == Program.Settings.DefaultBrokerId && e.Symbol == symbol);

            return asset;
        }

        public (string, string) GetAssetToBitGoAsync(IAssetIdentity assetId)
        {
            switch (assetId.Symbol)
            {
                case "BTC": return ("6054ba9ca9cc0e0024a867a7d8b401b2", "tbtc");
            }

            return (null, null);
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