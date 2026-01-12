using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.frame
{
    [DataContract, Serializable]
    public class FrameModel
    {

        [DataMember]
        private int sumLength; // 总长度 4B

        [DataMember]
        private byte endFrame; // 是否结束帧 1B

        [DataMember]
        private int frameIndex; // 帧索引 4B

        [DataMember]
        private byte frameType; // 帧类型 1B

        [DataMember]
        private byte fileType; // 文件类型 1B

        [DataMember]
        private byte operateType; // 文件操作类型 1B

        [DataMember]
        private int originDataBytesLength; // 实际数据长度4B

        [DataMember]
        private byte[] originDataBytes; // 原始数据 字节不确定

        [DataMember]
        private string data; // 解析出的数据




        // 解析中间过程数据
        [DataMember]
        private string status; // 状态

        [DataMember]
        private int index; // 当前解析对应的byte[]中索引

        [DataMember]
        private byte[] restBytes; // 剩余byte[]中


        // 文件字节流追加写入
        [DataMember]
        private int currentWriteIndex; // 当前文件流待写入的字节索引

        [DataMember]
        private int needToWriteBytesLength; // 需要写入的字节个数

        public FrameModel(string status, int index)
        {
            this.status = status;
            this.index = index;
        }

        public void setCurrentWriteIndex(int currentWriteIndex)
        {
            this.currentWriteIndex = currentWriteIndex;
        }

        public int getCurrentWriteIndex()
        {
            return this.currentWriteIndex;
        }

        public void setNeedToWriteBytesLength(int needToWriteBytesLength)
        {
            this.needToWriteBytesLength = needToWriteBytesLength;
        }

        public int getNeedToWriteBytesLength()
        {
            return this.needToWriteBytesLength;
        }

        public void setFrameIndex(int frameIndex)
        {
            this.frameIndex = frameIndex;
        }

        public int getFrameIndex()
        {
            return this.frameIndex;
        }

        public void setFileType(byte frameType)
        {
            this.frameType = frameType;
        }

        public byte getFileType()
        {
            return this.frameType;
        }

        public void setOperateType(byte operateType)
        {
            this.operateType = operateType;
        }

        public byte getOperateType()
        {
            return this.operateType;
        }

        public byte[] getRestBytes()
        {
            return this.restBytes;
        }

        public void setRestBytes(byte[] restBytes)
        {
            this.restBytes = restBytes;
        }

        public int getIndex()
        {
            return this.index;
        }

        public void setIndex(int index)
        {
            this.index = index;
        }

        public string getStatus()
        {
            return this.status;
        }

        public void setStatus(string status)
        {
            this.status = status;
        }

        public int getSumLength()
        {
            return this.sumLength;
        }

        public void setSumLength(int sumLength)
        {
            this.sumLength = sumLength;
        }

        public byte getFrameType()
        {
            return this.frameType;
        }

        public void setFrameType(byte frameType)
        {
            this.frameType = frameType;
        }

        public byte getEndFrame()
        {
            return this.endFrame;
        }

        public void setEndFrame(byte endFrame)
        {
            this.endFrame = endFrame;
        }

        public int getOriginDataBytesLength()
        {
            return this.originDataBytesLength;
        }

        public void setOriginDataBytesLength(int originDataBytesLength)
        {
            this.originDataBytesLength = originDataBytesLength;
        }

        public byte[] getOrigiDataBytes()
        {
            return this.originDataBytes;
        }

        public void setOrigiDataBytes(byte[] originDataBytes)
        {
            this.originDataBytes = originDataBytes;
        }

        public string getData()
        {
            return this.data;
        }

        public void setData(string data)
        {
            this.data = data;
        }

    }
}
