using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Maps;
using M3_Ybs;
using System.Net;

namespace M3_Ybs
{

    public class TCMSConnectionService : MPUListener.IM3YBSCommunicationCallback
    {






        public void A1VehicleDoorStatusChanged(bool A1VehicleDoorStatus)
        {
            M3_Ybs.GlobalVariablesDTO.A1_DoorStatus = A1VehicleDoorStatus;

            //Debug.WriteLine("A1VehicleDoorStatusChanged : " + A1VehicleDoorStatus.ToString());
        }

        public void A2VehicleDoorStatusChanged(bool A2VehicleDoorStatus)
        {
            //throw new NotImplementedException();
            M3_Ybs.GlobalVariablesDTO.A2_DoorStatus = A2VehicleDoorStatus;
        }
        public void AnnouncementStatusChanged(MPUListener.EnumsAnnouncement statusAnons)
        {
           

        }

        public void B1VehicleDoorStatusChanged(bool B1VehicleDoorStatus)
        {
            //Debug.WriteLine("B1VehicleDoorStatusChanged : " + B1VehicleDoorStatus.ToString());

            M3_Ybs.GlobalVariablesDTO.B1_DoorStatus = B1VehicleDoorStatus;

        }

        public void C1VehicleDoorStatusChanged(bool C1VehicleDoorStatus)
        {
            //throw new NotImplementedException();
            M3_Ybs.GlobalVariablesDTO.C1_DoorStatus = C1VehicleDoorStatus;
        }

        public void DD_CMileageKm_1Changed(int DD_CMileageKm_1)
        {
            //throw new NotImplementedException();

        }

        public void EVR_CResetDistChanged(bool EVR_CResetDist)
        {
            //throw new NotImplementedException();
            M3_Ybs.GlobalVariablesDTO.Discounter_Reset = EVR_CResetDist;
        }

        public void EVR_ICountDistChanged(int EVR_ICountDist)
        {
            double tako = Convert.ToDouble(EVR_ICountDist);
            M3_Ybs.GlobalVariablesDTO.Discounter = Convert.ToInt32(tako*GlobalVariablesDTO.takoKatSayisi);

            //Debug.WriteLine(EVR_ICountDist);
        }

        public void MPUConnectionStatusChanged(MPUListener.EnumsCommunication communication, MPUListener.EnumsConnection connection)
        {
            //Debug.WriteLine("{0} numaralı ürünün stok miktarında değişiklik oldu.");
        }

        public void NotifyMasterMPUForResetEVRDistanceStatus(string masterMPU, bool EVR_ICountDist)
        {
            //throw new NotImplementedException();
        }

        public void VB_DRS_OpenDoorsLeftChanged(bool VB_DRS_OpenDoorsLeft)
        {
            //throw new NotImplementedException();

            M3_Ybs.GlobalVariablesDTO.LeftDoor_Status = VB_DRS_OpenDoorsLeft;
        }

        public void VB_DRS_OpenDoorsRightChanged(bool VB_DRS_OpenDoorsRight)
        {
            //throw new NotImplementedException();
            M3_Ybs.GlobalVariablesDTO.RightDoor_Status = VB_DRS_OpenDoorsRight;
        }

        public void VB_DRS_TLLeftDrsReleasedChanged(bool VB_DRS_TLLeftDrsReleased)
        {
            //throw new NotImplementedException(); REleasee durumuuu
            M3_Ybs.GlobalVariablesDTO.All_LeftDoor_Release = VB_DRS_TLLeftDrsReleased;
        }

        public void VB_DRS_TLRightDrsReleasedChanged(bool VB_DRS_TLRightDrsReleased)
        {
            //throw new NotImplementedException();REleasee durumuuu
            M3_Ybs.GlobalVariablesDTO.All_RightDoor_Release = VB_DRS_TLRightDrsReleased;
        }

        public void VI_TBS_TrainSpeedKphChanged(int VI_TBS_TrainSpeedKph)
        {
            //throw new NotImplementedException();   Tren hızı
            M3_Ybs.GlobalVariablesDTO.TrenSpeed= VI_TBS_TrainSpeedKph; 
        }

    }
}
