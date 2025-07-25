﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum PacketType : int
{
    LOGIN = 0x0001,
    LOGIN_OK = 0x0002,
    KEY = 0x0011,
    CONNECT = 0x0012,
    HEART = 0x0021,
    BEAT = 0x0022,
}

public class Packet
{
    public PacketType packetType;
    public byte[] IV = new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    public byte[] tag = new byte[16] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    public byte[] data;

    public byte[] ToBytes()
    {
        List<byte> bytes = new List<byte>();

        // PacketType
        bytes.AddRange(BitConverter.GetBytes((int)packetType));

        // IV 길이 + IV
        bytes.AddRange(BitConverter.GetBytes(IV.Length));
        bytes.AddRange(IV);

        // Tag 길이 + Tag
        bytes.AddRange(BitConverter.GetBytes(tag.Length));
        bytes.AddRange(tag);

        // Data 길이 + Data
        bytes.AddRange(BitConverter.GetBytes(data.Length));
        bytes.AddRange(data);

        return bytes.ToArray();
    }

    public static Packet FromBytes(byte[] buffer)
    {
        Packet packet = new Packet();
        int offset = 0;

        // PacketType
        packet.packetType = (PacketType)BitConverter.ToInt32(buffer, offset);
        offset += 4;

        // IV
        int ivLen = BitConverter.ToInt32(buffer, offset);
        offset += 4;
        packet.IV = new byte[ivLen];
        Buffer.BlockCopy(buffer, offset, packet.IV, 0, ivLen);
        offset += ivLen;

        // Tag
        int tagLen = BitConverter.ToInt32(buffer, offset);
        offset += 4;
        packet.tag = new byte[tagLen];
        Buffer.BlockCopy(buffer, offset, packet.tag, 0, tagLen);
        offset += tagLen;

        // Data
        int dataLen = BitConverter.ToInt32(buffer, offset);
        offset += 4;
        packet.data = new byte[dataLen];
        Buffer.BlockCopy(buffer, offset, packet.data, 0, dataLen);

        return packet;
    }
}
