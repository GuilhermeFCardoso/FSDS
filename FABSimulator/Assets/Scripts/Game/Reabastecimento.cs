using System;
using FABSim.Core;
using UnityEngine;

namespace FABSim.Game
{
    // KC-390 em orbita no ponto de encontro; ao ver o F-39 segue na proa do destino com a cesta estendida.
    public sealed class Reabastecimento
    {
        const double Altitude = 6100, Velocidade = 175, RaioOrbita = 9000, KgPorSegundo = 22;
        const float RaioPega = 2.6f, RaioSolta = 14f, Visivel = 60000f;
        static readonly Vector3 PodoLocal = new Vector3(10.5f, 0.4f, -6f), CestaLocal = new Vector3(10.5f, -3.5f, -40f), PontaSonda = new Vector3(0.62f, 0.75f, 7.6f);

        readonly Combate k;
        readonly FlightController f;
        readonly V3 centro, proa;
        readonly double capacidade;
        readonly GameObject tanque;
        readonly AircraftRig rigTanque;
        readonly FlightState estadoTanque = new FlightState { Gear = 0, N = 0.7 };
        readonly Transform cesta, mangueira, sonda;
        double angulo, distAnterior;
        bool rumoDireto, disseProbe;

        public V3 Pos { get; private set; }
        public V3 Dir { get; private set; }
        public double Transferido { get; private set; }
        public bool Contato { get; private set; }
        public bool Feito { get; private set; }
        public Vector3 CestaMundo => cesta.position;
        public Vector3 PontaMundo => f.Fit.Root.transform.TransformPoint(PontaSonda);
        public double Distancia { get; private set; }
        public double Fechamento { get; private set; }
        public Vector3 Erro { get; private set; }
        public double Capacidade => capacidade;

        public Reabastecimento(Combate k, FlightController f, MissaoDef m, double capacidade)
        {
            this.k = k; this.f = f; this.capacidade = capacidade;
            centro = k.ToEnu(m.EncontroLat * Math.PI / 180, m.EncontroLon * Math.PI / 180, Altitude);
            var destino = BaseDb.Find(m.Destino);
            var alvo = k.ToEnu(destino.lat * Math.PI / 180, destino.lon * Math.PI / 180, Altitude);
            proa = Cm.Norm(new V3(alvo.X - centro.X, alvo.Y - centro.Y, 0));
            Pos = new V3(centro.X + RaioOrbita, centro.Y, Altitude);
            Dir = new V3(0, 1, 0);

            tanque = new GameObject("Tanque KC-390");
            rigTanque = AircraftRig.Build(ModelFit.Spawn("KC390", tanque.transform), "KC390", AircraftDb.KC390().Prepare());
            var metal = Resources.Load<Material>("FabMetal");
            Transform Cilindro(string nome, Transform pai)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                UnityEngine.Object.Destroy(go.GetComponent<Collider>());
                go.name = nome;
                go.GetComponent<Renderer>().sharedMaterial = metal;
                go.transform.SetParent(pai, false);
                return go.transform;
            }
            foreach (float lado in new[] { 1f, -1f })
            {
                var pod = Cilindro("Casulo", tanque.transform);
                pod.localPosition = new Vector3(lado * PodoLocal.x, PodoLocal.y, PodoLocal.z);
                pod.localRotation = Quaternion.Euler(90, 0, 0);
                pod.localScale = new Vector3(0.45f, 1.4f, 0.45f);
            }
            cesta = Cilindro("Cesta", tanque.transform);
            cesta.localScale = new Vector3(0.8f, 0.3f, 0.8f);
            mangueira = Cilindro("Mangueira", tanque.transform);
            sonda = Cilindro("Sonda", f.Fit.Root.transform);
            sonda.localPosition = PontaSonda + Vector3.back * 0.9f;
            sonda.localRotation = Quaternion.Euler(90, 0, 0);
            sonda.localScale = new Vector3(0.09f, 0.9f, 0.09f);
            sonda.gameObject.SetActive(false);
        }

        public void Atualizar(float dt)
        {
            sonda.gameObject.SetActive(f.ProbeOut);
            var pl = k.Sess.Pl.Pos;
            double perto = Cm.Dist(new V3(Pos.X, Pos.Y, 0), new V3(pl.X, pl.Y, 0));
            if (!rumoDireto && perto < 18000)
            {
                rumoDireto = true;
                Dir = proa;
                k.Radio.Diz("Tanque", "{indicativo}, Tanque um, visual com você. Mantendo 175 metros por segundo na proa do destino. Sonda fora e aproxime pela cesta direita.");
            }
            if (rumoDireto) Pos += Dir * (Velocidade * dt);
            else
            {
                angulo += Velocidade / RaioOrbita * dt;
                Pos = new V3(centro.X + RaioOrbita * Math.Cos(angulo), centro.Y + RaioOrbita * Math.Sin(angulo), Altitude);
                Dir = new V3(-Math.Sin(angulo), Math.Cos(angulo), 0);
            }

            bool mostra = perto < Visivel;
            if (tanque.activeSelf != mostra) tanque.SetActive(mostra);
            rigTanque.Apply(estadoTanque, dt);
            tanque.transform.position = k.World(Pos);
            tanque.transform.rotation = Quaternion.LookRotation(Combate.Dir(Dir), Vector3.up);

            var ponta = PontaMundo;
            var repouso = tanque.transform.TransformPoint(CestaLocal + new Vector3(Mathf.Sin(Time.time * 0.7f) * 0.35f, Mathf.Sin(Time.time * 0.9f) * 0.25f, 0));
            float d = (ponta - repouso).magnitude;
            Distancia = d;
            Fechamento = dt > 0 ? (distAnterior - d) / dt : 0;
            distAnterior = d;
            Erro = f.model.InverseTransformDirection(repouso - ponta);

            if (!disseProbe && !f.ProbeOut && d < 800 && !Feito) { disseProbe = true; k.Radio.Diz("Tanque", "{indicativo}, estenda a sonda de reabastecimento."); }
            if (!Contato && !Feito && f.ProbeOut && d < RaioPega)
            {
                Contato = true;
                k.Radio.Diz("Tanque", "Contato. Combustível transferindo. Mantenha a posição.");
            }
            else if (Contato && (d > RaioSolta || !f.ProbeOut))
            {
                Contato = false;
                k.Radio.Diz("Tanque", "Desconectado. Volte para a posição de pré-contato.");
            }
            if (Contato)
            {
                double antes = f.S.Fuel;
                f.S.Fuel = Math.Min(capacidade, f.S.Fuel + KgPorSegundo * dt);
                Transferido += f.S.Fuel - antes;
                if (f.S.Fuel >= capacidade * 0.97)
                {
                    Contato = false; Feito = true;
                    k.Radio.Diz("Tanque", $"Transferência concluída: {Transferido:0} quilos. Boa viagem, {{indicativo}}. Livre para seguir ao destino.");
                }
            }
            cesta.position = Contato ? ponta : repouso;
            cesta.rotation = tanque.transform.rotation * Quaternion.Euler(90, 0, 0);
            var inicio = tanque.transform.TransformPoint(PodoLocal);
            var fim = cesta.position;
            mangueira.position = (inicio + fim) / 2;
            mangueira.rotation = Quaternion.FromToRotation(Vector3.up, fim - inicio);
            mangueira.localScale = new Vector3(0.1f, (fim - inicio).magnitude / 2, 0.1f);
        }
    }
}
