﻿using ERBingoRandomizer.Utility;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static ERBingoRandomizer.Utility.Config;

namespace ERBingoRandomizer.FileHandler;

public class BHD5Reader {
    private const string Data0 = "Data0";
    private const string Data1 = "Data1";
    private const string Data2 = "Data2";
    private const string Data3 = "Data3";
    private static readonly string Data0CachePath = $"{CachePath}/{Data0}";
    private static readonly string Data1CachePath = $"{CachePath}/{Data1}";
    private static readonly string Data2CachePath = $"{CachePath}/{Data2}";
    private static readonly string Data3CachePath = $"{CachePath}/{Data3}";

    private readonly BHD5 _data0;
    private readonly BHD5 _data1;
    private readonly BHD5 _data2;
    private readonly BHD5 _data3;

    private readonly Dictionary<ulong, BHDInfo> _fileDictionary;
    public BHD5Reader(string path, bool cache, CancellationToken cancellationToken) {

        if (!Directory.Exists(CachePath)) {
            Directory.CreateDirectory(CachePath);
        }

        bool cacheExists = File.Exists(Data0CachePath);
        byte[][] msbBytes = new byte[4][];
        List<Task> tasks = new();
        switch (cacheExists) {
            case false:
                tasks.Add(Task.Run(() => { msbBytes[0] = CryptoUtil.DecryptRsa($"{path}/{Data0}.bhd", Const.ArchiveKeys.DATA0, cancellationToken).ToArray(); }));
                break;
            default:
                msbBytes[0] = File.ReadAllBytes(Data0CachePath);
                break;
        }
        // if (!File.Exists(Data1CachePath)) {
        //     tasks.Add(Task.Run(() => {
        //         msbBytes[1] = CryptoUtil.DecryptRsa($"{path}/{Data1}.bhd", Const.ArchiveKeys.DATA1).ToArray();
        //     }));
        // }
        // if (!File.Exists(Data2CachePath)) {
        //     tasks.Add(Task.Run(() => {
        //         msbBytes[2] = CryptoUtil.DecryptRsa($"{path}/{Data2}.bhd", Const.ArchiveKeys.DATA2).ToArray();
        //     }));
        // }
        // if (!File.Exists(Data3CachePath)) {
        //     tasks.Add(Task.Run(() => {
        //         msbBytes[3] = CryptoUtil.DecryptRsa($"{path}/{Data3}.bhd", Const.ArchiveKeys.DATA3).ToArray();
        //     }));
        // }
        try {
            Task.WaitAll(tasks.ToArray(), cancellationToken);
        }
        catch (AggregateException) {
            if (!cancellationToken.IsCancellationRequested) {
                throw;
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        _data0 = readBHD5(msbBytes[0]);
        cancellationToken.ThrowIfCancellationRequested();

        if (cache && !cacheExists) {
            File.WriteAllBytes($"{Data0CachePath}.bhd", msbBytes[0]);
        }
        // _data1 = readBHD5(msbBytes[1]);
        // _data2 = readBHD5(msbBytes[2]);
        // _data3 = readBHD5(msbBytes[3]);
        cancellationToken.ThrowIfCancellationRequested();

        _fileDictionary = new Dictionary<ulong, BHDInfo>();
        foreach (BHD5.Bucket bucket in _data0.Buckets) {
            foreach (BHD5.FileHeader header in bucket) {
                _fileDictionary.Add(header.FileNameHash, new BHDInfo(_data0, $"{path}/{Data0}"));
            }
        }
        // foreach (BHD5.Bucket bucket in _data1.Buckets) {
        //     foreach (BHD5.FileHeader header in bucket) {
        //         _fileDictionary.Add(header.FileNameHash, new BHDInfo(_data1, $"{path}/{Data1}"));
        //     }
        // }
        // foreach (BHD5.Bucket bucket in _data2.Buckets) {
        //     foreach (BHD5.FileHeader header in bucket) {
        //         _fileDictionary.Add(header.FileNameHash, new BHDInfo(_data2, $"{path}/{Data2}"));
        //     }
        // }
        // foreach (BHD5.Bucket bucket in _data3.Buckets) {
        //     foreach (BHD5.FileHeader header in bucket) {
        //         _fileDictionary.Add(header.FileNameHash, new BHDInfo(_data3, $"{path}/{Data3}"));
        //     }
        // }
    }
    private static BHD5 readBHD5(string path) {
        using FileStream fs = new(path, FileMode.Open);
        return BHD5.Read(fs, BHD5.Game.EldenRing);
    }
    private static BHD5 readBHD5(byte[] bytes) {
        using MemoryStream fs = new(bytes);
        return BHD5.Read(fs, BHD5.Game.EldenRing);
    }
    public byte[]? GetFile(string filePath) {
        ulong hash = Util.ComputeHash(filePath, BHD5.Game.EldenRing);
        if (!_fileDictionary.TryGetValue(hash, out BHDInfo? bhdInfo)) {
            return null;
        }
        Debug.WriteLine($"{filePath} : {bhdInfo.GetSalt()}");
        return bhdInfo.GetFile(hash);
    }
}
