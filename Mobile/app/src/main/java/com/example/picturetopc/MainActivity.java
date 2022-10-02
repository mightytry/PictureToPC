package com.example.picturetopc;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.icu.util.Calendar;
import android.os.AsyncTask;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ProgressBar;

import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.*;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;


class ConnSender extends Thread {
    ConnListener Listener;

    List<byte[]> Sendable;

    public ConnSender(ConnListener listener){
        Listener = listener;
    }

    public void run() {
        Sendable = new ArrayList<>();

        while (!Listener.Stop){

            if (!Listener.Blocked){
                SendAll();
            }
        }
    }



    private void SendAll() {
        if (Sendable.isEmpty()) return;

        Listener.Blocked = !Listener.Blocked;

        int size = 32768;

        byte[] send = Sendable.remove(0);

        Listener.Callable.Handler.ProgressBar.setMax(send.length);
        try {
            for (int i = 0; i < send.length; i += size) {
                Listener.Callable.Handler.ProgressBar.setProgress(i);
                int to = i + size;
                if (i + size >= send.length) to = send.length;
                byte[] data = Arrays.copyOfRange(send, i, to);
                Listener.Output.write(data);
            }
        } catch (IOException e) {
            Listener.Disconnect();
            return;
        }
    }

    public void Send(Bitmap bmp){
        int size = bmp.getRowBytes() * bmp.getHeight();
        System.out.println(size + "," + bmp.getHeight() + "," + bmp.getRowBytes());
        ByteBuffer byteBuffer = ByteBuffer.allocate(size);
        bmp.copyPixelsToBuffer(byteBuffer);
        byte[] byteArray = byteBuffer.array();

        Sendable.add((byteArray.length + "," + bmp.getHeight() + "," + bmp.getRowBytes()).getBytes(StandardCharsets.UTF_8));
        Sendable.add(byteArray);

    }
}


class ConnReader extends Thread
{
    ConnListener Listener;

    public ConnReader(ConnListener listener){
        Listener = listener;
    }

    public void run() {

        while (!Listener.Stop){
            try {
                byte[] bytes = new byte[1024];

                if (Listener.Input.read(bytes, 0, 1024) == -1) Listener.Disconnect();

                int i;
                for (i = 0; i < bytes.length && bytes[i] != 0; i++) { }


                String msg = new String(bytes, 0, i);


                switch (msg){
                    case "ready":
                        Listener.Blocked = false;
                        break;

                    default:
                        Listener.Disconnect();
                        break;
                }


            } catch (IOException e) {
                Listener.Disconnect();
            }
        }
    }
}

class ConnListener extends AsyncTask {
    String Ip;
    int Port = 42069;


    Socket Socket;
    InputStream Input;
    OutputStream Output;

    boolean Blocked;

    ConnReader Reader;
    ConnSender Sender;



    Connection Callable;
    boolean Stop;

    public ConnListener(String ip, Connection callable){
        Ip = ip;
        Callable = callable;

        Stop = false;
        Blocked = true;
    }


    @Override
    protected Object doInBackground(Object[] objects) {
        try {
            Socket = new Socket();
            Socket.connect(new InetSocketAddress(Ip, Port), 1000);

            Input = Socket.getInputStream();

            Output = Socket.getOutputStream();

            Callable.OnMessage("hi");
        }
        catch(Exception e)
        {
            Disconnect();
            return null;
        }

        Reader = new ConnReader(this);
        Sender = new ConnSender(this);

        Reader.start();
        Sender.start();

        return null;
    }



    public void Disconnect(){
        Callable.OnDisconnect();
    }

    public void Send(Bitmap bmp){
        Sender.Send(bmp);
    }

}


class Connection {
    IoHandler Handler;
    String Ip;

    ConnListener Listener;

    public Connection(String ip, IoHandler handler){
        Ip = ip;
        Handler = handler;
    }

    public void Connect(){
        Listener = new ConnListener(Ip, this);

        Listener.execute();
    }

    public void OnMessage(String msg)
    {
        if (msg == "hi"){
            Handler.Message(0);
        }
    }

    public void OnDisconnect() {
        Handler.Message(1);
        try {
            Listener.Socket.close();
        } catch (Exception e) {
            System.out.println(e);
        }
        Listener.Stop = true;
    }

}

class Listener implements View.OnClickListener {
    IoHandler Handler;

    public Listener(IoHandler handler){
        Handler = handler;
    }

    @Override
    public void onClick(View view) {
        Handler.OnClick(view);
    }
}



class IoHandler {
    String Ip;
    SharedPreferences.Editor Editor;
    EditText EditText;
    Button Button;
    ProgressBar ProgressBar;
    MainActivity Main;

    Connection Conn;
    private boolean connected;

    public IoHandler(String ip, SharedPreferences.Editor editor, EditText editText, Button button, ProgressBar progessBar, MainActivity main){
        Ip = ip;
        Editor = editor;
        EditText = editText;
        Button = button;
        ProgressBar =progessBar;
        Main = main;

        button.setOnClickListener(new Listener(this));
        connected = false;
        Load();

    }
    public void Load(){
        EditText.setText(Ip);
    }

    public void Save() {
        Editor.putString("IP", EditText.getText().toString());
        Editor.commit();
    }

    public void OnClick(View view) {

        if (!connected) {
            Button.setText("Connecting!");
            Save();
            Conn = new Connection(EditText.getText().toString(), this);
            Conn.Connect();
        }
        else{
            Main.GetImage();
        }

    }

    public void onPicture(Bitmap pic){
        Conn.Listener.Send(pic);

    }

    public void Message(int msg){
        switch (msg){
            case 0:
                connected = true;
                Button.setText("Connected!");
                break;
            case 1:
                connected = false;
                Conn = null;
                Button.setText("Connection Lost!");
        }
    }
}


public class MainActivity extends AppCompatActivity {

    private IoHandler connection;

    private Intent intent;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);



        SharedPreferences sharedPreferences = getPreferences(MODE_PRIVATE);

        final String ip = sharedPreferences.getString("IP", null);

        SharedPreferences.Editor editor = sharedPreferences.edit();

        connection = new IoHandler(ip, editor, findViewById(R.id.IpAdress), findViewById(R.id.ConnectBtn), findViewById(R.id.progressBar),this);
    }

    @Override
    protected void onRestart() {
        super.onRestart();

        String pic = "img.bmp";
        String dir = getExternalCacheDir().getAbsolutePath();

        System.out.println(dir+ "/"+pic);

        Bitmap bmp = BitmapFactory.decodeFile(dir+ "/"+pic);
        System.out.println(bmp);
        connection.onPicture(bmp);


    }

    public void GetImage(){
        intent = new Intent(this, Camera.class);

        this.startActivity(intent);



    }

    @Override
    protected void onDestroy(){
        super.onDestroy();

        String pic = "img.bmp";
        String dir = getExternalCacheDir().getAbsolutePath();
        new File(dir+ "/"+pic).delete();


        connection.Save();
    }
}