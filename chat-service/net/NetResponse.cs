using chat_service.frame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chat_service.net
{
    public class NetResponse
    {
        private Response response;

        // 结果和错误
        private string result, error;

        private object commonRes;

        // 响应类型枚举
        public enum Response
        {
            CONNECTION_SUCCESS = 100, CONNECTION_EXCEPTION = 101, RECEIVE_EXCEPTION = 110,
            SUCCESS = 200, 
            EMPTY_DATA = 300
        }

        public NetResponse()
        {

        }

        public NetResponse(string result)
        {
            this.response = Response.SUCCESS;
            this.result = result;
            this.error = "";
        }

        // 通用构造
        public static NetResponse of(Response response, object obj, string result, string error)
        {
            NetResponse netReponse = new NetResponse();
            netReponse.setResponse(response);
            netReponse.setCommonRes(obj);
            netReponse.setResult(result);
            netReponse.setError(error);
            return netReponse;
        }

        // 构造连接成功
        public static NetResponse ofConnectionSuccess(string result)
        {
            NetResponse netReponse = new NetResponse();
            netReponse.setResponse(Response.CONNECTION_SUCCESS);
            netReponse.setResult(result);
            netReponse.setError("");
            return netReponse;
        }

        // 构造连接失败
        public static NetResponse ofConnectFail(string error)
        {
            NetResponse netReponse = new NetResponse();
            netReponse.setResponse(Response.CONNECTION_EXCEPTION);
            netReponse.setResult("");
            netReponse.setError(error);
            return netReponse;
        }

        // 构造连接未处于连接状态异常
        public static NetResponse ofConnectStatusFail(string error)
        {
            NetResponse netReponse = new NetResponse();
            netReponse.setResponse(Response.CONNECTION_EXCEPTION);
            netReponse.setResult("");
            netReponse.setError(error);
            return netReponse;
        }

        // 构造接受数据异常
        public static NetResponse ofReceiveFail(string error)
        {
            NetResponse netReponse = new NetResponse();
            netReponse.setResponse(Response.RECEIVE_EXCEPTION);
            netReponse.setResult("");
            netReponse.setError(error);
            return netReponse;
        }

        public Response getResponse()
        {
            return this.response;
        }

        public void setResponse(Response response)
        {
            this.response = response;
        }

        public object getCommonRes()
        {
            return this.commonRes;
        }

        public void setCommonRes(object commonRes)
        {
            this.commonRes = commonRes;
        }

        public string getResult()
        {
            return this.result;
        }

        public void setResult(string result)
        {
            this.result = result;
        }

        public string getError()
        {
            return this.error;
        }

        public void setError(string error)
        {
            this.error = error;
        }
    }
}
