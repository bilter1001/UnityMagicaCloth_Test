// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using Unity.Collections;

namespace MagicaCloth
{
    /// <summary>
    /// NativeMultiHashMapの機能拡張版
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ExNativeMultiHashMap<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        /// <summary>
        /// ネイティブハッシュマップ
        /// </summary>
        NativeMultiHashMap<TKey, TValue> nativeMultiHashMap;

        /// <summary>
        /// ネイティブリストの配列数
        /// ※ジョブでエラーが出ないように事前に確保しておく
        /// </summary>
        int nativeLength;

        /// <summary>
        /// 使用キーの記録
        /// </summary>
        Dictionary<TKey, int> useKeyDict = new Dictionary<TKey, int>();

        //=========================================================================================
        public ExNativeMultiHashMap()
        {
            nativeMultiHashMap = new NativeMultiHashMap<TKey, TValue>(1, Allocator.Persistent);
            nativeLength = NativeCount;
        }

        public void Dispose()
        {
            if (nativeMultiHashMap.IsCreated)
            {
                nativeMultiHashMap.Dispose();
            }
            nativeLength = 0;
        }

        private int NativeCount
        {
            get
            {
#if UNITY_2019_3_OR_NEWER
                return nativeMultiHashMap.Count();
#else
                return nativeMultiHashMap.Length;
#endif
            }
        }

        //=========================================================================================
        public bool IsCreated
        {
            get
            {
                return nativeMultiHashMap.IsCreated;
            }
        }

        /// <summary>
        /// データ追加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            nativeMultiHashMap.Add(key, value);

            if (useKeyDict.ContainsKey(key))
                useKeyDict[key] = useKeyDict[key] + 1;
            else
                useKeyDict[key] = 1;

            nativeLength = NativeCount;
        }

        /// <summary>
        /// データ削除
        /// データ削除にはコストがかかるので注意！
        /// そして何故かこの関数は削除するごとに重くなる性質があるらしい（何故？）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Remove(TKey key, TValue value)
        {
            TValue data;
            NativeMultiHashMapIterator<TKey> iterator;
            if (nativeMultiHashMap.TryGetFirstValue(key, out data, out iterator))
            {
                do
                {
                    if (data.Equals(value))
                    {
                        // 削除
                        nativeMultiHashMap.Remove(iterator);

                        var cnt = useKeyDict[key] - 1;
                        if (cnt == 0)
                            useKeyDict.Remove(key);

                        break;
                    }
                }
                while (nativeMultiHashMap.TryGetNextValue(out data, ref iterator));
            }

            nativeLength = NativeCount;
        }

        /// <summary>
        /// 条件判定削除
        /// 何故か削除はこちらで一括でやったほうが早い!
        /// </summary>
        /// <param name="func">trueを返せば削除</param>
        public void Remove(Func<TKey, TValue, bool> func)
        {
            List<TKey> removeKey = new List<TKey>();
            foreach (TKey key in useKeyDict.Keys)
            {
                TValue data;
                NativeMultiHashMapIterator<TKey> iterator;
                if (nativeMultiHashMap.TryGetFirstValue(key, out data, out iterator))
                {
                    do
                    {
                        // 削除判定
                        if (func(key, data))
                        {
                            // 削除
                            nativeMultiHashMap.Remove(iterator);

                            var cnt = useKeyDict[key] - 1;
                            if (cnt == 0)
                                removeKey.Add(key);
                        }
                    }
                    while (nativeMultiHashMap.TryGetNextValue(out data, ref iterator));
                }
            }

            foreach (var key in removeKey)
                useKeyDict.Remove(key);

            nativeLength = NativeCount;
        }

        /// <summary>
        /// データ置き換え
        /// </summary>
        /// <param name="func">trueを返せば置換</param>
        /// <param name="rdata">引数にデータを受け取り、修正したデータを返し置換する</param>
        public void Replace(Func<TKey, TValue, bool> func, Func<TValue, TValue> datafunc)
        {
            foreach (var key in useKeyDict.Keys)
            {
                TValue data;
                NativeMultiHashMapIterator<TKey> iterator;
                if (nativeMultiHashMap.TryGetFirstValue(key, out data, out iterator))
                {
                    do
                    {
                        // 置換判定
                        if (func(key, data))
                        {
                            // 置き換え
                            nativeMultiHashMap.SetValue(datafunc(data), iterator);
                            return;
                        }
                    }
                    while (nativeMultiHashMap.TryGetNextValue(out data, ref iterator));
                }
            }
            nativeLength = NativeCount;
        }

        /// <summary>
        /// データに対してアクションを実行
        /// </summary>
        /// <param name="act"></param>
        public void Process(Action<TKey, TValue> act)
        {
            foreach (var key in useKeyDict.Keys)
            {
                TValue data;
                NativeMultiHashMapIterator<TKey> iterator;
                if (nativeMultiHashMap.TryGetFirstValue(key, out data, out iterator))
                {
                    do
                    {
                        act(key, data);
                    }
                    while (nativeMultiHashMap.TryGetNextValue(out data, ref iterator));
                }
            }
        }

        /// <summary>
        /// キーのデータに対してアクションを実行
        /// </summary>
        /// <param name="key"></param>
        /// <param name="act"></param>
        public void Process(TKey key, Action<TValue> act)
        {
            TValue data;
            NativeMultiHashMapIterator<TKey> iterator;
            if (nativeMultiHashMap.TryGetFirstValue(key, out data, out iterator))
            {
                do
                {
                    act(data);
                }
                while (nativeMultiHashMap.TryGetNextValue(out data, ref iterator));
            }
        }

        /// <summary>
        /// データが存在するか判定する
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(TKey key, TValue value)
        {
            TValue data;
            NativeMultiHashMapIterator<TKey> iterator;
            if (nativeMultiHashMap.TryGetFirstValue(key, out data, out iterator))
            {
                do
                {
                    if (data.Equals(value))
                    {
                        return true;
                    }
                }
                while (nativeMultiHashMap.TryGetNextValue(out data, ref iterator));
            }

            return false;
        }

        /// <summary>
        /// キーが存在するか判定する
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(TKey key)
        {
            return useKeyDict.ContainsKey(key);
        }

        /// <summary>
        /// キーの削除
        /// </summary>
        /// <param name="key"></param>
        public void Remove(TKey key)
        {
            nativeMultiHashMap.Remove(key);
            useKeyDict.Remove(key);
            nativeLength = NativeCount;
        }


        /// <summary>
        /// 実際に利用されている要素数を返す
        /// </summary>
        public int Count
        {
            get
            {
                //return nativeMultiHashMap.Length;
                return nativeLength;
            }
        }

        /// <summary>
        /// データ削除
        /// </summary>
        public void Clear()
        {
            nativeMultiHashMap.Clear();
            nativeLength = 0;
            useKeyDict.Clear();
        }

        /// <summary>
        /// 内部のNativeMultiHashMapを取得する
        /// </summary>
        /// <returns></returns>
        public NativeMultiHashMap<TKey, TValue> Map
        {
            get
            {
                return nativeMultiHashMap;
            }
        }
    }
}
