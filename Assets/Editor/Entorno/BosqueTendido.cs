using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheFog.EditorTools.Entorno
{
    /// <summary>
    /// Tendido eléctrico del bosque: postes del ramal con el travesaño perpendicular a la línea y los cables que
    /// los unen (cuatro hilos, uno por aislador, con su comba), la acometida a la casa, la bajada a la torre y la
    /// manguera del generador. Los postes son un modelo (Tripo); el cable no puede serlo porque cada vano mide
    /// distinto: es una malla de tubo fino calculada entre los aisladores y guardada como asset.
    /// </summary>
    public static partial class BosqueBuilder
    {
        const string CableMesh = KitDir + "Tendido/Cables.asset";
        const string CableMat = MatDir + "Cable.mat";
        const float CableRadio = 0.02f;
        const float Comba = 0.028f;          // flecha del cable, en proporción a la longitud del vano
        const float CalleTendido = 7.5f;    // sin árboles a esta distancia de la línea (los cables no cruzan ramas)

        // Tramos de la línea en planta (ax, az, bx, bz): bajo ellos no se plantan árboles.
        static readonly List<Vector4> vanos = new List<Vector4>();

        static bool BajoElTendido(Vector2 p, float margen)
        {
            foreach (var v in vanos)
                if (EntornoLayout.SegmentDistance(p, new Vector2(v.x, v.y), new Vector2(v.z, v.w)) < margen) return true;
            return false;
        }

        static void Tendido(Transform root, GameObject casa, GameObject torre, Vector3 frenteCasa)
        {
            vanos.Clear();
            // Sitios de los postes: a un lado del ramal, de la casa hacia la torre.
            var sitios = new List<Vector2>(); var giros = new List<float>();
            var ramal = Senderos[1];
            float acumulado = 0f, siguiente = 9f;
            for (int i = 0; i < ramal.Length - 1; i++)
            {
                Vector2 a = ramal[i], b = ramal[i + 1], dir = (b - a).normalized, normal = new Vector2(-dir.y, dir.x);
                float len = Vector2.Distance(a, b);
                while (siguiente < acumulado + len)
                {
                    Vector2 p = a + dir * (siguiente - acumulado) + normal * 3.4f;
                    if (!EnEdificio(p, 2f) && ClaroDistance(p) > -11f) { sitios.Add(p); giros.Add(Random.Range(-4f, 4f)); }
                    siguiente += 21f;
                }
                acumulado += len;
            }
            if (sitios.Count == 0) return;

            // Cada poste mira de través a la línea: el travesaño (eje z del modelo) queda perpendicular a los cables.
            var postes = Group(root, "Postes");
            Vector2 casaXZ = new Vector2(casa.transform.position.x + frenteCasa.x * 4f, casa.transform.position.z + frenteCasa.z * 4f);
            var cabezas = new List<Vector3[]>(); var gos = new List<GameObject>();
            for (int k = 0; k < sitios.Count; k++)
            {
                Vector2 antes = k == 0 ? casaXZ : sitios[k - 1], despues = k == sitios.Count - 1 ? TorrePos : sitios[k + 1];
                Vector2 d = (despues - antes).normalized;
                var poste = Prop(TripoDir + "poste_luz/poste_luz.gltf", postes, sitios[k], Yaw(new Vector3(d.x, 0, d.y)) + 90f + giros[k], 1f, false, 0.6f);
                if (poste == null) return;
                var c = poste.AddComponent<CapsuleCollider>(); c.direction = 1; c.radius = 0.18f; c.height = 6f;
                c.center = poste.transform.InverseTransformPoint(poste.transform.position + Vector3.up * 3f);
                gos.Add(poste); cabezas.Add(Aisladores(poste));
            }
            Physics.SyncTransforms();

            var v = new List<Vector3>(); var tri = new List<int>();
            cableNormales.Clear();
            // Entre postes: hilo i de uno con hilo i del siguiente.
            for (int k = 0; k < sitios.Count - 1; k++)
            {
                for (int i = 0; i < 4; i++) Vano(v, tri, cabezas[k][i], cabezas[k + 1][i], Comba);
                vanos.Add(new Vector4(sitios[k].x, sitios[k].y, sitios[k + 1].x, sitios[k + 1].y));
            }

            // Acometida a la casa: los hilos de cada lado del travesaño se juntan en un punto del hastial, sobre el porche.
            {
                var cab = cabezas[0];
                Vector3 fin = PicoDelTejado(casa, frenteCasa);
                for (int i = 0; i < 4; i++) Vano(v, tri, cab[i], fin, Comba * 0.7f);
                vanos.Add(new Vector4(sitios[0].x, sitios[0].y, casa.transform.position.x, casa.transform.position.z));
            }

            // Bajada a la torre: a la pata más cercana, dos puntos uno sobre otro; de ahí baja por la pata hasta el suelo.
            {
                int u = sitios.Count - 1; var cab = cabezas[u];
                Vector3 medio = (cab[1] + cab[2]) * 0.5f;
                Vector3 pie, alto;
                if (Pata(torre, medio, out pie, out alto))
                {
                    // Un par de centímetros hacia el poste, para que el cable vaya por fuera de la pata y no dentro.
                    Vector3 fuera = new Vector3(medio.x - pie.x, 0f, medio.z - pie.z).normalized * (CableRadio * 1.5f);
                    pie += fuera; alto += fuera;
                    float y1 = medio.y - 0.9f, y2 = y1 - 0.45f;
                    Vector3 p1 = EnPata(pie, alto, y1), p2 = EnPata(pie, alto, y2);
                    Vano(v, tri, cab[0], p1, Comba * 0.7f); Vano(v, tri, cab[1], p1, Comba * 0.7f);
                    Vano(v, tri, cab[2], p2, Comba * 0.7f); Vano(v, tri, cab[3], p2, Comba * 0.7f);
                    Vector3 abajo = EnPata(pie, alto, pie.y + 0.12f);
                    Tubo(v, tri, new[] { p1, p2, abajo }, CableRadio);
                    // Manguera del generador a la pata, por el suelo.
                    if (generadorGo != null)
                    {
                        Vector3 g = generadorGo.transform.position;
                        var suelo = new List<Vector3> { g + Vector3.up * 0.3f };
                        Vector2 a = new Vector2(g.x, g.z), b = new Vector2(abajo.x, abajo.z), n = new Vector2(-(b - a).y, (b - a).x).normalized;
                        for (int s = 1; s < 28; s++)
                        {
                            float t = s / 28f;
                            float onda = (Mathf.Sin(t * Mathf.PI * 2f) * 0.38f + Mathf.Sin(t * Mathf.PI * 5f + 1.3f) * 0.12f) * Mathf.Sin(t * Mathf.PI);
                            suelo.Add(Suelo(Vector2.Lerp(a, b, t) + n * onda, 0f) + Vector3.up * CableRadio);
                        }
                        suelo.Add(abajo);
                        Tubo(v, tri, suelo, CableRadio);
                    }
                }
                else Debug.LogWarning("[Bosque] No encuentro la pata de la torre para la bajada del tendido.");
                vanos.Add(new Vector4(sitios[u].x, sitios[u].y, TorrePos.x, TorrePos.y));
            }

            // Una sola malla para todo el cable.
            EnsureFolder(System.IO.Path.GetDirectoryName(CableMesh).Replace('\\', '/'));
            AssetDatabase.DeleteAsset(CableMesh);
            var mesh = new Mesh { name = "Cables", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetNormals(cableNormales); mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, CableMesh);
            var m = AssetDatabase.LoadAssetAtPath<Material>(CableMat);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, CableMat); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit"); // goma negra: con luz se ve redondo, no una cinta plana
            m.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.055f));
            m.SetFloat("_Smoothness", 0.25f); m.SetFloat("_Metallic", 0f);
            m.SetFloat("_EnvironmentReflections", 0f); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            m.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(m);
            var go = new GameObject("Cables");
            go.transform.SetParent(postes, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            go.isStatic = true;
        }

        /// <summary>Los cuatro aisladores del travesaño, medidos en la malla del poste: el punto donde apoya el hilo.</summary>
        static Vector3[] Aisladores(GameObject poste)
        {
            var mf = poste.GetComponentInChildren<MeshFilter>();
            var vs = mf.sharedMesh.vertices;
            float[][] bandas = { new[] { -2.6f, -1.95f }, new[] { -1.95f, -1.3f }, new[] { 1.0f, 1.75f }, new[] { 1.75f, 2.6f } }; // en z local
            var res = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 cima = new Vector3(0f, float.MinValue, 0f);
                foreach (var p in vs) if (p.z >= bandas[i][0] && p.z < bandas[i][1] && p.y > cima.y) cima = p;
                if (cima.y == float.MinValue) cima = new Vector3(0f, mf.sharedMesh.bounds.max.y, (bandas[i][0] + bandas[i][1]) * 0.5f);
                res[i] = mf.transform.TransformPoint(cima + Vector3.down * 0.1f);
            }
            return res;
        }

        static readonly List<Vector3> cableNormales = new List<Vector3>();

        /// <summary>Punta delantera de la cumbrera de la casa: ahí se recogen los cuatro hilos de la acometida.</summary>
        static Vector3 PicoDelTejado(GameObject casa, Vector3 frente)
        {
            Vector3 mejor = casa.transform.position + Vector3.up * 3.5f; float nota = float.MinValue;
            foreach (var mf in casa.GetComponentsInChildren<MeshFilter>())
            {
                var r = mf.GetComponent<Renderer>();
                if (mf.sharedMesh == null || r == null || !r.sharedMaterials.Any(m => m != null && m.name.StartsWith("Rooftile"))) continue;
                foreach (var p in mf.sharedMesh.vertices)
                {
                    Vector3 w = mf.transform.TransformPoint(p);
                    float n = w.y * 4f + Vector3.Dot(w - casa.transform.position, frente); // lo más alto y, de eso, lo más adelantado
                    if (n > nota) { nota = n; mejor = w; }
                }
            }
            return mejor + Vector3.down * 0.04f;
        }

        /// <summary>
        /// La pata de la torre más cercana a un punto, por una de sus aristas: en la malla de la estructura, las patas
        /// son las únicas aristas largas y casi verticales. Pie y cabeza son los dos extremos de la misma arista.
        /// </summary>
        static bool Pata(GameObject torre, Vector3 cerca, out Vector3 pie, out Vector3 alto)
        {
            pie = alto = Vector3.zero;
            float d = float.MaxValue;
            foreach (Transform t in torre.transform)
            {
                if (t.name != "Cube_016" && t.name != "Cube_046") continue;
                var mf = t.GetComponent<MeshFilter>();
                if (mf == null) continue;
                var vs = mf.sharedMesh.vertices; var tr = mf.sharedMesh.triangles;
                for (int i = 0; i < tr.Length; i += 3)
                for (int e = 0; e < 3; e++)
                {
                    Vector3 a = t.TransformPoint(vs[tr[i + e]]), b = t.TransformPoint(vs[tr[i + (e + 1) % 3]]);
                    if (a.y > b.y) { var x = a; a = b; b = x; }
                    float dy = b.y - a.y, dh = new Vector2(b.x - a.x, b.z - a.z).magnitude;
                    if (dy < 6f || dh / dy > 0.25f || b.y < cerca.y) continue;
                    float dd = (a.x - cerca.x) * (a.x - cerca.x) + (a.z - cerca.z) * (a.z - cerca.z) + dh * 0.01f;
                    if (dd < d) { d = dd; pie = a; alto = b; }
                }
            }
            return d < float.MaxValue;
        }

        static Vector3 EnPata(Vector3 pie, Vector3 alto, float y) => Vector3.Lerp(pie, alto, Mathf.InverseLerp(pie.y, alto.y, y));

        /// <summary>Hilo colgado entre dos puntos, con su comba.</summary>
        static void Vano(List<Vector3> v, List<int> tri, Vector3 a, Vector3 b, float comba)
        {
            const int N = 14;
            float flecha = Vector3.Distance(a, b) * comba;
            var pts = new Vector3[N + 1];
            for (int i = 0; i <= N; i++)
            {
                float t = i / (float)N;
                pts[i] = Vector3.Lerp(a, b, t) + Vector3.down * (flecha * 4f * t * (1f - t));
            }
            Tubo(v, tri, pts, CableRadio);
        }

        /// <summary>Tubo fino de cinco caras a lo largo de una línea de puntos.</summary>
        static void Tubo(List<Vector3> v, List<int> tri, IList<Vector3> pts, float radio)
        {
            const int L = 5;
            int n = pts.Count, b0 = v.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 t = (pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 refv = Mathf.Abs(t.y) > 0.9f ? Vector3.right : Vector3.up;
                Vector3 a = Vector3.Cross(refv, t).normalized, b = Vector3.Cross(t, a);
                for (int s = 0; s < L; s++)
                {
                    float ang = s * Mathf.PI * 2f / L;
                    Vector3 radial = a * Mathf.Cos(ang) + b * Mathf.Sin(ang);
                    v.Add(pts[i] + radial * radio);
                    cableNormales.Add(radial);
                }
            }
            for (int i = 0; i < n - 1; i++)
            for (int s = 0; s < L; s++)
            {
                int A = b0 + i * L + s, B = b0 + i * L + (s + 1) % L, C = A + L, D = B + L;
                tri.Add(A); tri.Add(B); tri.Add(C);
                tri.Add(B); tri.Add(D); tri.Add(C);
            }
        }
    }
}
