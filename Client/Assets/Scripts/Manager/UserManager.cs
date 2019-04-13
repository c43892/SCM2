using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;
using System.Linq;

namespace SCM
{
    public class UserManager : Component
    {
        public static Action onSyncUnits2Server = null;
        public static Action onSyncUUnlocksFromCfg = null;

        // ��ʼ��
        public override void Init()
        {
            UnitConfigUtil.OnBuildUnitCfgsFromServer += SyncUnitsFromCfg;

            onSyncUnits2Server += SyncUUnlocksFromCfg;
        }

        // �����ñ�ͬ��ͷ�������Ϣ
        public static void SyncAvatarsFromCfg()
        {
            var allAvas = AvatarConfiguration.Cfgs;

            var myAvas = GameCore.Instance.MeInfo.Avatars;

            for (int i = 0; i < allAvas.Count; i++)
            {
                if (!myAvas.ContainsKey(allAvas[i]))
                {
                    myAvas.Add(allAvas[i], false);
                }
            }

            SyncAvatars2Server();
        }

        // ͬ��������ͷ�������Ϣ
        public static void SyncAvatars2Server()
        {
            var info = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrAvatars");

            var aKeys = info.Avatars.Keys.ToArray();

            buff.Write(aKeys.Length);

            foreach (var a in aKeys)
            {
                buff.Write(a);
                buff.Write(info.Avatars[a]);
            }

            conn.End(buff);
        }

        // ͬ����������ǰͷ����Ϣ
        public static void SyncCurAvatar2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrCurAvatar");

            buff.Write(meInfo.CurAvator);

            conn.End(buff);
        }

        // �����ñ�ͬ����λ������Ϣ
        public static void SyncUnitsFromCfg()
        {
            var meInfo = GameCore.Instance.MeInfo;
            var keys = UnitConfiguration.AllUnitTypes;

            for (int i = 0; i < keys.Length; i++)
            {
                if (!meInfo.Units.ContainsKey(keys[i]))
                {
                    meInfo.Units[keys[i]] = false;
                }
            }

            SyncUnits2Server();
        }

        // ͬ����������λ������Ϣ
        public static void SyncUnits2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrUnits");

            var uKeys = meInfo.Units.Keys.ToArray();

            buff.Write(uKeys.Length);

            foreach (var u in uKeys)
            {
                buff.Write(u);
                buff.Write(meInfo.Units[u]);
            }

            conn.End(buff);

            if (onSyncUnits2Server != null)
                onSyncUnits2Server();
        }

        // �����ñ�ͬ�����ֵ�λ��Ϣ
        public static void SyncVariantsFromCfg()
        {
            var info = GameCore.Instance.MeInfo;
            var orgKeys = UnitConfiguration.AllOriginalUnitTypes;

            for (int i = 0; i < orgKeys.Length; i++)
            {
                if (!info.Variants.ContainsKey(orgKeys[i]))
                {
                    info.Variants[orgKeys[i]] = orgKeys[i];
                }
                else
                {
                    var vcfg = UnitConfiguration.GetDefaultConfig(info.Variants[orgKeys[i]]);

                    if ((null == vcfg) || (vcfg.OriginalType != orgKeys[i]))
                    {
                        info.Variants[orgKeys[i]] = orgKeys[i];
                    }
                }
            }

            SyncVariants2Server();
        }

        // ͬ�����������ֵ�λ��Ϣ
        public static void SyncVariants2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrVariants");

            var vkeys = meInfo.Variants.Keys.ToArray();

            buff.Write(vkeys.Length);

            foreach (var k in vkeys)
            {
                buff.Write(k);
                buff.Write(meInfo.Variants[k]);
            }

            conn.End(buff);
        }

        // �����ñ�ͬ����λ��������
        public static void SyncUUnlocksFromCfg()
        {
            var uUlocks = GameCore.Instance.MeInfo.UUnlocks;
            var ulcfgs = UnitConfiguration.Ulcfgs;

            for (int i = 0; i < ulcfgs.Count; i++)
            {
                if (!uUlocks.ContainsKey(ulcfgs[i]))
                {
                    uUlocks.Add(ulcfgs[i], false);
                }
            }

            SyncUUnlocks2Server();
        }

        // ͬ����������λ��������
        public static void SyncUUnlocks2Server()
        {
            var info = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrUUlocks");

            var uKeys = info.UUnlocks.Keys.ToArray();

            buff.Write(uKeys.Length);

            foreach (var a in uKeys)
            {
                buff.Write(a);
                buff.Write(info.UUnlocks[a]);
            }

            conn.End(buff);
        }

        // ���ʽ���һ�����ͷ��
        public static string UnlockOneAvatarAtRandom()
        {
            //�ɹ�����100% ����(0-99)
            var successRate = 9;

            var num = new Random().Next(100);

            if (num > successRate)
                return null;

            // ����δ������ͷ���б�
            var info = GameCore.Instance.MeInfo;
            var aKeys = info.Avatars.Keys.ToArray();

            List<string> lst = new List<string>();

            foreach (var a in aKeys)
            {
                if (!info.Avatars[a])
                    lst.Add(a);
            }

            if (lst.Count == 0)
                return null;

            var index = new Random().Next(0, lst.Count - 1);

            info.Avatars[lst[index]] = true;
            SyncAvatars2Server();

            return lst[index];
        }

        // ������λ
        public static bool UnlockUnit(string type)
        {
            var meInfo = GameCore.Instance.MeInfo;
            var ulcfgs = UnitConfiguration.Ulcfgs;

            for (int i = 0; i < ulcfgs.Count; i++)
            {
                if (!meInfo.UUnlocks[ulcfgs[i]])
                {
                    var left = meInfo.Integration - meInfo.IntegrationCost;
                    var need = ulcfgs[i] - ulcfgs[i - 1];

                    if (left >= need)
                    {
                        // �������ɹ�
                        meInfo.Units[type] = true;
                        meInfo.UUnlocks[ulcfgs[i]] = true;

                        SyncUnits2Server();
                        SyncUUnlocks2Server();

                        return true;
                    }
                    else
                        return false;
                }
            }

            return false;
        }

        // ���Դ�����λ����
        public static string TryTriggerUUnlock()
        {
            var meInfo = GameCore.Instance.MeInfo;
            var ulcfgs = UnitConfiguration.Ulcfgs;
            var cfgs = UnitConfiguration.AllUnitTypes;

            // ����δ������λ�б�
            var lst = new List<string>();

            foreach (var k in cfgs)
            {
                if (!meInfo.Units[k])
                    lst.Add(k);
            }

            if (lst.Count == 0)
                return null;

            for (int i = 0; i < ulcfgs.Count; i++)
            {
                if (meInfo.Integration >= ulcfgs[i] && !meInfo.UUnlocks[ulcfgs[i]])
                {
                    var index = new Random().Next(0, lst.Count - 1);

                    meInfo.Units[lst[index]] = true;
                    meInfo.UUnlocks[ulcfgs[i]] = true;

                    SyncUnits2Server();
                    SyncUUnlocks2Server();
                    return lst[index];
                }
            }

            return null;
        }

        // ͬ��������������Ϣ
        public static void SyncName2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrName");
            buff.Write(meInfo.Name);
            conn.End(buff);
        }

        // ͬ������������ֵ
        public static void SyncIntegration2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrIntegration");
            buff.Write(meInfo.Integration);
            conn.End(buff);
        }

        // ͬ���������ѻ��ѻ���ֵ
        public static void SyncIntegrationCost2Server()
        {
            var meInfo = GameCore.Instance.MeInfo;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ModifyUsrIntegrationCost");
            buff.Write(meInfo.IntegrationCost);
            conn.End(buff);
        }
    }
}