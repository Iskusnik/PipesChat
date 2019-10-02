using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Pipes
{
    public partial class frmMain : Form
    {

        private List<ClientInfo> Clients;

        private Int32 PipeHandle;                                                       // дескриптор канала
        private string PipeName = "\\\\" + Dns.GetHostName() + "\\pipe\\ServerPipe";    // имя канала, Dns.GetHostName() - метод, возвращающий имя машины, на которой запущено приложение
        private Thread t;                                                               // поток для обслуживания канала
        private bool _continue = true;                                                  // флаг, указывающий продолжается ли работа с каналом

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            Clients = new List<ClientInfo>(10);

            // создание именованного канала
            PipeHandle = DIS.Import.CreateNamedPipe("\\\\.\\pipe\\ServerPipe", DIS.Types.PIPE_ACCESS_DUPLEX | DIS.Types.OVERLAPPED, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);

            // вывод имени канала в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Text += "     " + PipeName;
            
            // создание потока, отвечающего за работу с каналом
            t = new Thread(ReceiveMessage);
            t.Start();
        }

        unsafe private void ReceiveMessage()
        {
            string msg = "";            // прочитанное сообщение
            uint realBytesReaded = 0;   // количество реально прочитанных из канала байтов

            // входим в бесконечный цикл работы с каналом
            while (_continue)
            {
                
                if (DIS.Import.ConnectNamedPipe(PipeHandle, 0))//, DIS.Types.OVERLAPPED
                {
                    byte[] buff = new byte[1024];                                           // буфер прочитанных из канала байтов
                    DIS.Import.FlushFileBuffers(PipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                    DIS.Import.ReadFile(PipeHandle, buff, 1024, ref realBytesReaded,0);    // считываем последовательность байтов из канала в буфер buff
                    msg = Encoding.Unicode.GetString(buff);                                 // выполняем преобразование байтов в последовательность символов
                    
                        if (msg != "")
                        {

                            string[] messageInfo = msg.Split('_');
                            switch (messageInfo[0])
                            {
                                case "reg":
                                    Clients.Add(new ClientInfo(messageInfo[2], messageInfo[1]));
                                    rtbMessages.Invoke((MethodInvoker)delegate
                                    {
                                        rtbMessages.Text += "\n >> в чат зашёл >>" + messageInfo[2];
                                        rtbMessages.Text += "\n >> info >>" + messageInfo[1];//
                                    });
                                    break;

                                case "msg":
                                    rtbMessages.Invoke((MethodInvoker)delegate
                                    {
                                        rtbMessages.Text += "\n >> " + messageInfo[1] + " >> by " + messageInfo[2];         // выводим полученное сообщение на форму 
                                        Thread.Sleep(200);
                                    });

                                    //отправляем всем клиентам
                                    string clientMessage = "\n >> " + messageInfo[1] + " >> by " + messageInfo[2];
                                    foreach (ClientInfo client in Clients)
                                    {
                                        SendMessage(clientMessage, client.PipeName);
                                        Thread.Sleep(200);
                                    }
                                    break;
                            }
                            
                        
                        }
                    

                    DIS.Import.DisconnectNamedPipe(PipeHandle);                             // отключаемся от канала клиента 
                    Thread.Sleep(500);                                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                }
            }
        }
        private void SendMessage(string msg, string serverPipe)
        {
            rtbMessages.Invoke((MethodInvoker)delegate
            {
                rtbMessages.Text += "\n отправлено в " + serverPipe;
            });

            Thread.Sleep(200);
            uint BytesWritten = 0;  // количество реально записанных в канал байт
            byte[] buff = Encoding.Unicode.GetBytes(msg);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            // открываем именованный канал, имя которого указано serverPipe
            Int32 PipeHandleSendMsg = DIS.Import.CreateFile(serverPipe, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandleSendMsg, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandleSendMsg);
            Thread.Sleep(200);
        }
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с каналом завершена

            if (t != null)
                t.Abort();          // завершаем поток
            
            if (PipeHandle != -1)
                DIS.Import.CloseHandle(PipeHandle);     // закрываем дескриптор канала
        }
    }
}