﻿using System;
using System.Collections.Generic;
using Stratis.Bitcoin.P2P.Peer;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.P2P.Protocol.Behaviors
{
    public interface INetworkPeerBehavior
    {
        NetworkPeer AttachedPeer { get; }
        void Attach(NetworkPeer peer);
        void Detach();
        INetworkPeerBehavior Clone();
    }

    public abstract class NetworkPeerBehavior : INetworkPeerBehavior
    {
        private object cs = new object();
        private List<IDisposable> disposables = new List<IDisposable>();
        public NetworkPeer AttachedPeer { get; private set; }

        protected abstract void AttachCore();

        protected abstract void DetachCore();

        public abstract object Clone();

        protected void RegisterDisposable(IDisposable disposable)
        {
            this.disposables.Add(disposable);
        }

        public void Attach(NetworkPeer peer)
        {
            Guard.NotNull(peer, nameof(peer));

            if (this.AttachedPeer != null)
                throw new InvalidOperationException("Behavior already attached to a peer");

            lock (this.cs)
            {
                this.AttachedPeer = peer;
                if (Disconnected(peer))
                    return;

                this.AttachCore();
            }
        }

        protected void AssertNotAttached()
        {
            if (this.AttachedPeer != null)
                throw new InvalidOperationException("Can't modify the behavior while it is attached");
        }

        private static bool Disconnected(NetworkPeer peer)
        {
            return (peer.State == NetworkPeerState.Disconnecting) || (peer.State == NetworkPeerState.Failed) || (peer.State == NetworkPeerState.Offline);
        }

        public void Detach()
        {
            lock (this.cs)
            {
                if (this.AttachedPeer == null)
                    return;

                this.DetachCore();
                foreach (IDisposable dispo in this.disposables)
                    dispo.Dispose();

                this.disposables.Clear();
                this.AttachedPeer = null;
            }
        }

        INetworkPeerBehavior INetworkPeerBehavior.Clone()
        {
            return (INetworkPeerBehavior)Clone();
        }
    }
}