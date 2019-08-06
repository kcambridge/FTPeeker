# FTPeeker

A web based SFTP client that allows users to access SFTP sites without sharing the credentials. It is built using **ASP.NET MVC**, **C#** and **SQL Server**

## Features

* SFTP Browsing
* File Upload
* File Download
* Sorting
* Filtering
* Upload and Download Logging

## Setup

1. Fill in **YOUR_DATABASE_NAME** in the **setup.sql** file and execute it on your your SQL database
2. Insert SFTP sites into the newly created **FTPK_FTPs** table
3. Update the **connection string** in the **web.config** file with your **server name**, **database name**, **user name** and **password**

## Logging

Activity is logged to the **FTPK_Logs** table
