# Hololens-Communication

**Sender is running on PC, Reciver is running on Hololens.**

This project was to achieve the communication between a Hololens and a PC running window10 (Lenovo Yoga). The main goal was to manipulate 3D objects in Hololens through the touch surface on Yoga, including dragging, rotating and scaling.

```
UDPController.cs 
```
This file is included in both two folders which is the essence of the communication part.

```
BTController.cs 
```
I have also included a BlueTooth controller in case a high data transmission speed is required. However Hololens turns out not very friendly for using Bluetooth transmission. Each time when rebuild the solution, I need to modify settings in Hololens otherwise the transmission would be blocked. 

In case someone who wants to use Bluetooth, here is how to modify setting in Hololens:

```
Settings -> Privacy -> Other devices -> Let these apps use my #Device-Name# (turn on your apps)
```
