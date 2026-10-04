// Desktop adaptation of oneko.js, Copyright (c) 2022 adryd. MIT license.
using System;
using System.Collections.Generic;

public sealed class NekoEngine
{
    public double X, Y;
    public string Sprite = "idle";
    public int SpriteFrame;
    public double Speed = 10;
    public double StopDistance = 48;
    public double Margin = 16;
    int frameCount, idleTime, idleFrame;
    string idleAnimation;
    readonly Random random;

    public NekoEngine(double x, double y, int seed)
    { X = x; Y = y; random = new Random(seed); }

    public void Tick(double mouseX, double mouseY, double left, double top, double right, double bottom)
    {
        frameCount = (frameCount + 1) % 1000000;
        double dx = X - mouseX, dy = Y - mouseY;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < Speed || distance < StopDistance)
        { Idle(left, top, right, bottom); return; }
        idleAnimation = null;
        idleFrame = 0;
        if (idleTime > 1)
        {
            SetSprite("alert", 0);
            idleTime = Math.Min(idleTime, 7) - 1;
            return;
        }
        string direction = dy / distance > 0.5 ? "N" : "";
        direction += dy / distance < -0.5 ? "S" : "";
        direction += dx / distance > 0.5 ? "W" : "";
        direction += dx / distance < -0.5 ? "E" : "";
        SetSprite(direction, frameCount);
        X -= dx / distance * Speed;
        Y -= dy / distance * Speed;
        Clamp(left, top, right, bottom);
    }

    public void Clamp(double left, double top, double right, double bottom)
    {
        X = Math.Min(Math.Max(left + Margin, X), right - Margin);
        Y = Math.Min(Math.Max(top + Margin, Y), bottom - Margin);
    }

    public void ResetIdle() { idleTime = 0; idleFrame = 0; idleAnimation = null; Sprite = "idle"; SpriteFrame = 0; }

    void SetSprite(string name, int frame) { Sprite = name; SpriteFrame = frame; }

    void Idle(double left, double top, double right, double bottom)
    {
        idleTime = Math.Min(idleTime + 1, 1000000);
        if (idleTime > 10 && random.Next(200) == 0 && idleAnimation == null)
        {
            var choices = new List<string> { "sleeping", "scratchSelf" };
            double edge = Margin * 2;
            if (X < left + edge) choices.Add("scratchWallW");
            if (Y < top + edge) choices.Add("scratchWallN");
            if (X > right - edge) choices.Add("scratchWallE");
            if (Y > bottom - edge) choices.Add("scratchWallS");
            idleAnimation = choices[random.Next(choices.Count)];
        }
        if (idleAnimation == "sleeping")
        {
            if (idleFrame < 8) SetSprite("tired", 0);
            else SetSprite("sleeping", idleFrame / 4);
            if (idleFrame > 192) { idleAnimation = null; idleFrame = 0; }
        }
        else if (idleAnimation != null)
        {
            SetSprite(idleAnimation, idleFrame);
            if (idleFrame > 9) { idleAnimation = null; idleFrame = 0; }
        }
        else { SetSprite("idle", 0); return; }
        idleFrame++;
    }
}
