﻿namespace SideLoader
{
    public class SL_UseLoadoutAmunition : SL_Effect
    {
        public bool MainHand;

        public override void ApplyToComponent<T>(T component)
        {
            (component as UseLoadoutAmunition).MainHand = this.MainHand;
        }

        public override void SerializeEffect<T>(T effect)
        {
            MainHand = (effect as UseLoadoutAmunition).MainHand;
        }
    }
}
