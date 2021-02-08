// Magica Cloth.
// Copyright (c) MagicaSoft, 2020.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 共有データオブジェクトのプレハブ化処理
    /// プレハブがApplyされた場合に、自動でスクリプタブルオブジェクをプレハブのサブアセットとして保存します。
    /// 該当するコンポーネントにIShareDataObjectを継承し、GetAllShareDataObject()で該当する共有データを返す必要があります。
    /// </summary>
    [InitializeOnLoad]
    internal class ShareDataPrefabExtension
    {
        static List<GameObject> prefabInstanceList = new List<GameObject>();

        /// <summary>
        /// プレハブ更新コールバック登録
        /// </summary>
        static ShareDataPrefabExtension()
        {
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdate;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        /// <summary>
        /// プレハブステージが閉じる時
        /// </summary>
        /// <param name="obj"></param>
        static void OnPrefabStageClosing(PrefabStage pstage)
        {
            if (prefabInstanceList.Count > 0)
            {
                DelaySavePrefabAndConnect();
            }
        }

        /// <summary>
        /// プレハブモードでプレハブが保存される直前
        /// </summary>
        /// <param name="instance"></param>
        static void OnPrefabSaving(GameObject instance)
        {
            //Debug.Log("OnPrefabSaving()->" + instance.name);
            if (prefabInstanceList.Contains(instance) == false)
            {
                prefabInstanceList.Add(instance);
                DelaySavePrefabAndConnect();
            }
        }


        /// <summary>
        /// プレハブがApplyされた場合に呼ばれる
        /// instanceはヒエラルキーにあるゲームオブジェクト
        /// プレハブが更新された場合、スクリプタブルオブジェクをプレハブのサブアセットとして自動保存する
        /// </summary>
        /// <param name="instance"></param>
        static void OnPrefabInstanceUpdate(GameObject instance)
        {
            // プレハブモードではインスタンスが異なる
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                var pstage = PrefabStageUtility.GetCurrentPrefabStage();
                instance = pstage.prefabContentsRoot;
            }

            if (prefabInstanceList.Contains(instance))
                return;

            prefabInstanceList.Add(instance);

            EditorApplication.delayCall += DelaySavePrefabAndConnect;
        }

        /// <summary>
        /// プレハブのサブアセット更新
        /// OnPrefabInstanceUpdate()内部でプレハブの操作を行うと謎のエラーが発生するので、
        /// delayCallを使い画面更新後に遅延させて実行する
        /// </summary>
        static void DelaySavePrefabAndConnect()
        {
            EditorApplication.delayCall -= DelaySavePrefabAndConnect;

            foreach (var instance in prefabInstanceList)
            {
                if (instance)
                {
                    GameObject prefab = null;
                    string prefabPath = null;

                    if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                    {
                        var pstage = PrefabStageUtility.GetCurrentPrefabStage();
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pstage.prefabAssetPath);
                        prefabPath = pstage.prefabAssetPath;
                    }
                    else
                    {
                        prefab = PrefabUtility.GetCorrespondingObjectFromSource(instance) as GameObject;
                        prefabPath = AssetDatabase.GetAssetPath(prefab);
                    }
                    //Debug.Log("Prefab originPath=" + prefabPath);

                    if (prefab != null)
                        PrefabUpdate(prefab, instance, prefabPath);
                }
            }
            prefabInstanceList.Clear();
        }

        /// <summary>
        /// MagicaClothの共有データをサブアセットとしてプレハブに保存する
        /// またすでに不要なサブアセットは削除する
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="target"></param>
        /// <param name="prefabPath"></param>
        static void PrefabUpdate(GameObject prefab, GameObject target, string prefabPath)
        {
            //Debug.Log("PrefabUpdate()");
            //Debug.Log("prefab name:" + prefab.name);
            //Debug.Log("prefab id:" + prefab.GetInstanceID());
            //Debug.Log("target name:" + target.name);
            //Debug.Log("target id:" + target.GetInstanceID());
            //Debug.Log("prefab path:" + prefabPath);
            //Debug.Log("IsPersistent:" + EditorUtility.IsPersistent(prefab));
            //Debug.Log("AssetDatabase.Contains:" + AssetDatabase.Contains(prefab));
            //Debug.Log("IsPartOfModelPrefab:" + PrefabUtility.IsPartOfModelPrefab(prefab));
            //Debug.Log("IsPartOfPrefabThatCanBeAppliedTo:" + PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(prefab));
            //Debug.Log("IsPartOfImmutablePrefab:" + PrefabUtility.IsPartOfImmutablePrefab(prefab));


            // 編集不可のプレハブならば保存できないため処理を行わない
            if (PrefabUtility.IsPartOfImmutablePrefab(prefab))
                return;

            AssetDatabase.StartAssetEditing();

            // 不要な共有データを削除するためのリスト
            bool change = false;
            List<ShareDataObject> removeDatas = new List<ShareDataObject>();

            // 現在アセットとして保存されているすべてのShareDataObjectサブアセットを削除対象としてリスト化する
            UnityEngine.Object[] subassets = AssetDatabase.LoadAllAssetRepresentationsAtPath(prefabPath);
            if (subassets != null)
            {
                foreach (var obj in subassets)
                {
                    // ShareDataObjectのみ
                    ShareDataObject sdata = obj as ShareDataObject;
                    if (sdata)
                    {
                        //Debug.Log("sub asset:" + obj.name + " type:" + obj + " test:" + AssetDatabase.IsSubAsset(sdata));

                        // 削除対象として一旦追加
                        removeDatas.Add(sdata);
                    }
                }
            }

            // プレハブ元の共有オブジェクトをサブアセットとして保存する
            List<ShareDataObject> saveDatas = new List<ShareDataObject>();
            var shareDataInterfaces = target.GetComponentsInChildren<IShareDataObject>(true);
            if (shareDataInterfaces != null)
            {
                foreach (var sdataInterface in shareDataInterfaces)
                {
                    List<ShareDataObject> shareDatas = sdataInterface.GetAllShareDataObject();
                    if (shareDatas != null)
                    {
                        foreach (var sdata in shareDatas)
                        {
                            if (sdata)
                            {
                                //Debug.Log("share data->" + sdata.name + " prefab?:" + AssetDatabase.Contains(sdata));
                                //Debug.Log("share data path:" + AssetDatabase.GetAssetPath(sdata));

                                if (AssetDatabase.Contains(sdata) == false)
                                {
                                    // サブアセットとして共有データを追加
                                    //Debug.Log("save sub asset:" + sdata.name);
                                    AssetDatabase.AddObjectToAsset(sdata, prefab);
                                    change = true;
                                }
                                else if (prefabPath != AssetDatabase.GetAssetPath(sdata))
                                {
                                    // 保存先プレハブが変更されている
                                    //Debug.Log("Change prefab!!");

                                    // 共有データのクローンを作成（別データとする）
                                    var newdata = sdataInterface.DuplicateShareDataObject(sdata);
                                    if (newdata != null)
                                    {
                                        //Debug.Log("save new data! ->" + newdata);
                                        AssetDatabase.AddObjectToAsset(newdata, prefab);
                                        change = true;
                                    }
                                }

                                removeDatas.Remove(sdata);
                            }
                        }
                    }
                }
            }

            // 不要な共有データは削除する
            foreach (var sdata in removeDatas)
            {
                //Debug.Log("Remove sub asset:" + sdata.name);
                UnityEngine.Object.DestroyImmediate(sdata, true);
                change = true;
            }

            AssetDatabase.StopAssetEditing();

            // 変更を全体に反映
            if (change)
            {
                //Debug.Log("save!");

                // どうもこの手順を踏まないと保存した共有データが正しくアタッチされない
                if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    PrefabUtility.SaveAsPrefabAsset(target, prefabPath);
                }
                else
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(target, prefabPath, InteractionMode.AutomatedAction);
                }
            }
        }
    }
}
