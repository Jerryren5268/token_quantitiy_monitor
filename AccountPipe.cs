using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

class AccountPipe : IDisposable {
    Process worker; StreamWriter input; readonly object gate=new object(); bool closed;
    public static Dictionary<string,object> Command(string action){return new Dictionary<string,object>{{"action",action}};}
    public Task<Dictionary<string,object>> Call(Dictionary<string,object> args){return Task.Run(()=>{
        lock(gate){if(closed)throw new ObjectDisposedException("账户服务");
            if(worker==null){var root=AppDomain.CurrentDomain.BaseDirectory;var python=Path.Combine(root,@"runtime\python.exe");
                if(!File.Exists(python))python=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),@".cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe");
                if(!File.Exists(python))throw new Exception("缺少随包运行时，请重新解压完整安装包。");
                var p=new ProcessStartInfo(python,"-X utf8 -u \""+Path.Combine(root,"account_service.py")+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8};
                worker=Process.Start(p);input=new StreamWriter(worker.StandardInput.BaseStream,new UTF8Encoding(false)){AutoFlush=true};worker.ErrorDataReceived+=(s,e)=>{};worker.BeginErrorReadLine();}
            var json=new JavaScriptSerializer{MaxJsonLength=2000000};input.WriteLine(json.Serialize(args));var reading=worker.StandardOutput.ReadLineAsync();
            if(!reading.Wait(95000)){try{worker.Kill();}catch{};worker.Dispose();worker=null;throw new Exception("请求超时。若正在订阅，请先核实钱包再重试。");}
            var line=reading.Result;if(line==null){worker.Dispose();worker=null;throw new Exception("账户服务已退出，请重试。");}
            return json.Deserialize<Dictionary<string,object>>(line);
        }});}
    public void Dispose(){closed=true;try{if(worker!=null&&!worker.HasExited)worker.Kill();}catch{} }
}

