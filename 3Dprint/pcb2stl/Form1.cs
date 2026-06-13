using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace pcb2stl {
	public partial class Form1 : Form {
		public Form1() {
			InitializeComponent();
		}
		double[,] fline = new double[5000, 5];
		double[,] bline = new double[5000, 5];
		double[,] vline = new double[5000, 5];
		int[] fvia = new int[5000];
		int flinep, blinep, vlinep;
		//
		double p_fh = 0.3F; // スライス高さ
		double p_zb = 0.0F; // 描画開始高さ
		double p_zh = 0.6F; // 基板厚み
		double j_fb = 0.01F; // 上下位置一致判定値
		double p_zf = 3.0F; // ジャンパー配線高さ
		double p_jl = 1.5F; // ジャンプ台長さ
		double p_jw = 1.2F; // ジャンプ台幅係数
		double p_le = 0.3F; // 太さを基準にした配線延長係数
		//
		public static void GenerateStl(double[,] p, out string stltxt) {
			// 立方体の8つの頂点座標（例: 1辺が2の大きさ、原点中心）
			var vertices = new[]{
				new Tuple<double, double, double>(p[0,0],p[0,1],p[0,2]), // 0
				new Tuple<double, double, double>(p[1,0],p[1,1],p[1,2]), // 1
				new Tuple<double, double, double>(p[2,0],p[2,1],p[2,2]), // 2
				new Tuple<double, double, double>(p[3,0],p[3,1],p[3,2]), // 3
				new Tuple<double, double, double>(p[4,0],p[4,1],p[4,2]), // 4
				new Tuple<double, double, double>(p[5,0],p[5,1],p[5,2]), // 5
				new Tuple<double, double, double>(p[6,0],p[6,1],p[6,2]), // 6
				new Tuple<double, double, double>(p[7,0],p[7,1],p[7,2])  // 7
            };

			// キューブの6面を構成する12枚の三角形（頂点インデックスの組み合わせ）
			var triangles = new[]{
				new[] { 0, 3, 2 }, new[] { 0, 2, 1 }, // 下面
                new[] { 4, 5, 6 }, new[] { 4, 6, 7 }, // 上面
                new[] { 0, 1, 5 }, new[] { 0, 5, 4 }, // 前面
                new[] { 2, 3, 7 }, new[] { 2, 7, 6 }, // 背面
                new[] { 3, 0, 4 }, new[] { 3, 4, 7 }, // 左面
                new[] { 1, 2, 6 }, new[] { 1, 6, 5 }  // 右面
            };

			var sb = new StringBuilder();

			foreach (var tri in triangles) {
				var v1 = vertices[tri[0]];
				var v2 = vertices[tri[1]];
				var v3 = vertices[tri[2]];

				// 三角形の法線ベクトルを計算
				var normal = CalculateNormal(v1, v2, v3);

				sb.AppendLine($"  facet normal {normal.Item1} {normal.Item2} {normal.Item3}");
				sb.AppendLine("    outer loop");
				sb.AppendLine($"      vertex {v1.Item1} {v1.Item2} {v1.Item3}");
				sb.AppendLine($"      vertex {v2.Item1} {v2.Item2} {v2.Item3}");
				sb.AppendLine($"      vertex {v3.Item1} {v3.Item2} {v3.Item3}");
				sb.AppendLine("    endloop");
				sb.AppendLine("  endfacet");
			}

			stltxt = sb.ToString();
		}

		// 外積を用いた法線ベクトルの計算
		private static Tuple<double, double, double> CalculateNormal(
			Tuple<double, double, double> v1,
			Tuple<double, double, double> v2,
			Tuple<double, double, double> v3) {
			// 2つのベクトルの算出
			double ax = v2.Item1 - v1.Item1;
			double ay = v2.Item2 - v1.Item2;
			double az = v2.Item3 - v1.Item3;

			double bx = v3.Item1 - v1.Item1;
			double by = v3.Item2 - v1.Item2;
			double bz = v3.Item3 - v1.Item3;

			// 外積 (a x b)
			double nx = ay * bz - az * by;
			double ny = az * bx - ax * bz;
			double nz = ax * by - ay * bx;

			// 正規化 (単位ベクトル化)
			double length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
			if (length > 0) {
				nx /= length;
				ny /= length;
				nz /= length;
			}

			return new Tuple<double, double, double>(nx, ny, nz);
		}
		//
		//------------------------------------------------------

		private string box2stl(double x0, double y0, double x1, double y1, double fw, double zb) {
			double[,] cp = new double[8, 3];
			double zp = zb + fw;
			cp[0, 0] = x0; cp[0, 1] = y0; cp[0, 2] = zb;  // point 0
			cp[1, 0] = x1; cp[1, 1] = y0; cp[1, 2] = zb;  // point 1
			cp[2, 0] = x1; cp[2, 1] = y1; cp[2, 2] = zb;  // point 2
			cp[3, 0] = x0; cp[3, 1] = y1; cp[3, 2] = zb;  // point 3
			for (int i = 4; i < 8; i++) {
				cp[i, 0] = cp[i - 4, 0]; cp[i, 1] = cp[i - 4, 1]; cp[i, 2] = zp;
			}
			GenerateStl(cp, out string stltxt);
			return stltxt;
		}
		private string line2stl(int mode, double vw, double x0, double y0, double x1, double y1, double fw, double fh, double zb) {
			double[,] cp = new double[8, 3];
			//
			//int mode = sts / 4; sts = sts & 3;
			//
			double xl = x1 - x0;
			double yl = y1 - y0;
			double tl = Math.Sqrt(xl * xl + yl * yl);	// 斜辺の長さ
			double zp = zb + fh;						// ラインの高さ
			if(mode>0) zp = zp+ fh;						// viaの場合
			// 配線の太さを計算
			double rat = (fw / 2) / tl; // 相似形の比率
			double xs = yl * rat;       // xオフセット(90度回転しているから)
			double ys = xl * rat;       // y　　〃
			// ビアの大きさを計算
			double vra = (vw / 2) / tl; // ビアの比率
			double xv = yl * vra;       // ビアxオフセット
			double yv = xl * vra;       // ビアyオフセット
			double sra, vx, vy;			// 計算用変数
			if(mode==3 || mode==6 || mode==7) sra = (vw * 1.5) / tl;
			else                              sra = (vw * 0.5) / tl;
			vx = xl * sra;
			vy = yl * sra;
			// 上手く繋ぐ為に少し伸ばす
			double ex, xe, ye;
			if(mode==0) ex = fw * p_le;
			else        ex = (fw * p_le)+(vw/2);
			xe = xl * rat * ex;
			ye = yl * rat * ex;
			x0 = x0 - xe; y0 = y0 - ye;
			x1 = x1 + xe; y1 = y1 + ye;
			// 下の位置の数値を設定
			cp[0, 0] = x0 + xs; cp[0, 1] = y0 - ys; cp[0, 2] = zb;  // point 0
			cp[1, 0] = x1 + xs; cp[1, 1] = y1 - ys; cp[1, 2] = zb;  // point 1
			cp[2, 0] = x1 - xs; cp[2, 1] = y1 + ys; cp[2, 2] = zb;  // point 2
			cp[3, 0] = x0 - xs; cp[3, 1] = y0 + ys; cp[3, 2] = zb;  // point 3
			// 穴を開けるための補正														//
			if (mode == 1) {
				cp[0, 0] = x0 - xv; cp[0, 1] = y0 + yv;
				cp[1, 0] = x1 - xv; cp[1, 1] = y1 + yv;
			}
			if (mode == 2) {
				cp[2, 0] = x1 + xv; cp[2, 1] = y1 - yv;
				cp[3, 0] = x0 + xv; cp[3, 1] = y0 - yv;
			}
			if (mode == 3) {
				cp[1, 0] = cp[1, 0] - vx; cp[1, 1] = cp[1, 1] - vy;
				cp[2, 0] = cp[2, 0] - vx; cp[2, 1] = cp[2, 1] - vy;
			}
			if (mode == 4) {
				cp[0, 0] = cp[1, 0] - vx; cp[0, 1] = cp[1, 1] - vy;
				cp[3, 0] = cp[2, 0] - vx; cp[3, 1] = cp[2, 1] - vy;
			}
			if (mode == 5) {
				cp[1, 0] = cp[0, 0] + vx; cp[1, 1] = cp[0, 1] + vy;
				cp[2, 0] = cp[3, 0] + vx; cp[2, 1] = cp[3, 1] + vy;
			}
			if (mode == 6) {
				cp[0, 0] = cp[0, 0] + vx; cp[0, 1] = cp[0, 1] + vy;
				cp[3, 0] = cp[3, 0] + vx; cp[3, 1] = cp[3, 1] + vy;
			}
			if (mode == 7) {
				cp[0, 0] = cp[0, 0] + vx; cp[0, 1] = cp[0, 1] - vy;
				cp[1, 0] = cp[1, 0] - vx; cp[1, 1] = cp[1, 1] - vy;
				cp[2, 0] = cp[2, 0] - vx; cp[2, 1] = cp[2, 1] + vy;
				cp[3, 0] = cp[3, 0] + vx; cp[3, 1] = cp[3, 1] + vy;
			}
			// 上側を計算(下をコピー)
			for (int i = 4; i < 8; i++) {
				cp[i, 0] = cp[i - 4, 0]; cp[i, 1] = cp[i - 4, 1]; cp[i, 2] = zp;
			}
			GenerateStl(cp, out string stltxt);
			//System.Windows.Forms.Application.DoEvents();
			return stltxt;
		}

		private void get_num1(string ns, out double d0) {
			int cp1, cp2;
			string s1;
			cp1 = ns.IndexOf(" "); cp2 = ns.IndexOf(")", cp1 + 1);
			s1 = ns.Substring(cp1 + 1, cp2 - cp1 - 1); d0 = double.Parse(s1);
		}
		private void get_num2(string ns, out double d0, out double d1) {
			int cp1, cp2, cp3;
			string s1, s2;
			cp1 = ns.IndexOf(" "); cp2 = ns.IndexOf(" ", cp1 + 1); cp3 = ns.IndexOf(")", cp2 + 1);
			s1 = ns.Substring(cp1 + 1, cp2 - cp1 - 1); d0 = double.Parse(s1);
			s2 = ns.Substring(cp2 + 1, cp3 - cp2 - 1); d1 = double.Parse(s2);
		}
		private string conv_line(string fname) {
			int cp1, cp2, cp3, cp4, cp5;
			string? rstart, rend, rwidth, rlayer, rdumy;
			string stllst;
			StreamReader sr = new StreamReader(fname, Encoding.GetEncoding("UTF-8"));
			stllst = "";
			while (sr.EndOfStream == false) {
				string? scad = sr.ReadLine();
				cp1 = scad!.IndexOf("(gr_rect", StringComparison.OrdinalIgnoreCase);
				cp2 = scad!.IndexOf("(segment", StringComparison.OrdinalIgnoreCase);
				cp3 = scad!.IndexOf("(via", StringComparison.OrdinalIgnoreCase);
				//
				if (cp3 > 0) {
					rstart = sr.ReadLine();
					get_num2(rstart!, out double d0, out double d1);
					rdumy = sr.ReadLine(); rwidth = sr.ReadLine();
					get_num1(rwidth!, out double d3);   // drill
					d1 = -d1;
					vline[vlinep, 0] = d0; vline[vlinep, 1] = d1;
					vline[vlinep, 2] = 0; vline[vlinep, 3] = d3;
					vline[vlinep, 4] = 0; vlinep++;
				}
				//
				if ((cp1 >= 0 || cp2 >= 0)) {
					rstart = sr.ReadLine(); rend = sr.ReadLine();
					rwidth = sr.ReadLine(); rlayer = sr.ReadLine();
					get_num2(rstart!, out double d0, out double d1);
					get_num2(rend!, out double d2, out double d3);
					d1 = -d1; d3 = -d3;                                         // 上下反転(何故か必要)
																				// 基板ベースを描画
					if (cp1 >= 0) {
						stllst = stllst + box2stl(d0, d1, d2, d3, p_zh, p_zb);  // ベースを描画
						p_zb = p_zh;                                            // ベースを上昇
					}
					// 配線データを収集(背面配線用)
					if (cp2 >= 0) {
						cp4 = rlayer!.IndexOf("F.Cu", StringComparison.OrdinalIgnoreCase);
						cp5 = rlayer!.IndexOf("B.Cu", StringComparison.OrdinalIgnoreCase);
						get_num1(rwidth!, out double d4);
						if (cp4 >= 0) {
							fline[flinep, 0] = d0; fline[flinep, 1] = d1;
							fline[flinep, 2] = d2; fline[flinep, 3] = d3;
							fline[flinep, 4] = d4; flinep++;
						}
						if (cp5 >= 0) {
							bline[blinep, 0] = d0; bline[blinep, 1] = d1;
							bline[blinep, 2] = d2; bline[blinep, 3] = d3;
							bline[blinep, 4] = d4; blinep++;
						}
					}
				}
			}
			sr.Close();
			return stllst;
		}

		private string mk_jbase(int fno, int fp) {
			double x0 = fline[fno, 0]; double y0 = fline[fno, 1];
			double x1 = fline[fno, 2]; double y1 = fline[fno, 3];
			double xl = x1 - x0; double yl = y1 - y0;
			double tl = Math.Sqrt(xl * xl + yl * yl);
			if (tl > p_jl) {
				double rat = p_jl / tl;
				double xs = xl * rat; double ys = yl * rat;
				if (fp == 1) { x1 = x0 + xs; y1 = y0 + ys; } else { x0 = x1 - xs; y0 = y1 - ys; }
			}
			double d4 = fline[fno, 4];
			double jh = p_zb + d4 * p_zf;
			double jw = d4 * p_jw;
			string stltxt = line2stl(0, 0, x0, y0, x1, y1, jw, jh, p_zb);
			return stltxt;
		}
		private int chk_def(double x0, double y0, double x1, double y1) {
			double xdef = Math.Abs(x1 - x0);
			double ydef = Math.Abs(y1 - y0);
			if (xdef < ydef) xdef = ydef;
			if (xdef < j_fb) return 1;  // (0.2)mm以内なら一致とする
			else return -1;
		}
		private int chk_vconn(double xf, double yf) {
			double xv, yv;
			int rc = -1;
			for (int vc = 0; vc < vlinep; vc++) {
				xv = vline[vc, 0]; yv = vline[vc, 1];
				rc = chk_def(xf, yf, xv, yv);
				if (rc != -1) { rc = vc; return rc; }
			}
			return rc;
		}
		private int chk_fconn(double xf, double yf) {
			double xb, yb;
			int rc;
			for (int fc = 0; fc < flinep; fc++) {
				rc = 0;
				for (int pp = 0; pp < 4; pp = pp + 2) {
					xb = fline[fc, pp + 0]; yb = fline[fc, pp + 1];
					int cc = chk_def(xf, yf, xb, yb);
					if (cc != -1) rc = rc | (pp / 2 + 1);
				}
				if(rc!=0) return (fc * 4 + rc);
			}
			return -1;
		}
		private int chk_bconn(double xf, double yf) {
			double xb, yb;
			int rc;
			for (int bc = 0; bc < blinep; bc++) {
				rc = 0;
				for (int pp = 0; pp < 4; pp = pp + 2) {
					xb = bline[bc, pp + 0]; yb = bline[bc, pp + 1];
					int cc = chk_def(xf, yf, xb, yb);
					if (cc != -1) rc = rc | (pp / 2 + 1);
				}
				if (rc != 0) return (bc * 4 + rc);
			}
			return -1;
		}
		private string conv_jtbl() {
			string stllst;
			double xf, yf;
			int rcb, rcf;
			stllst = "";
			//for (int i = 0; i < 5000; i++) fvia[i] = 0;
			for (int vc = 0; vc < vlinep; vc++) {
				xf = vline[vc, 0]; yf = vline[vc, 1];
				rcf = chk_fconn(xf, yf);
				rcb = chk_bconn(xf, yf);
				if (rcf != -1) {
					if (rcb != -1)
						stllst = stllst + mk_jbase(rcf / 4, (rcf & 3));
					else
						fvia[rcf / 4] = fvia[rcf / 4] | (vc*4+(rcf & 3));    // viaは有るが相手不一致の場合
				}
			}
			return stllst;
		}
		private string make_line(int mode, double[,] xline, int xlinep) {
			string stllst;
			double d0, d1, d2, d3, d4;
			stllst = "";
			for (int lp = 0; lp < xlinep; lp++) {
				d0 = xline[lp, 0]; d1 = xline[lp, 1];
				d2 = xline[lp, 2]; d3 = xline[lp, 3];
				d4 = xline[lp, 4];
				if (mode == 1) {
					// ベースに書く
					int pp = fvia[lp];
					if (pp == 0)
						// そのまま
						stllst = stllst + line2stl(0, 0, d0, d1, d2, d3, d4, d4, p_zb);
					else {
						double vw = vline[pp/4, 3];	pp = pp & 3;
						// 穴をあける。
						stllst = stllst + line2stl(1, vw, d0, d1, d2, d3, d4, d4, p_zb);
						stllst = stllst + line2stl(2, vw, d0, d1, d2, d3, d4, d4, p_zb);
						if (pp == 2) {
							stllst = stllst + line2stl(3, vw, d0, d1, d2, d3, d4, d4, p_zb);
							stllst = stllst + line2stl(4, vw, d0, d1, d2, d3, d4, d4, p_zb);
						}
						if (pp == 1) { 
							stllst = stllst + line2stl(5, vw, d0, d1, d2, d3, d4, d4, p_zb);
							stllst = stllst + line2stl(6, vw, d0, d1, d2, d3, d4, d4, p_zb);
						}
						if (pp == 3) { // 両側に穴が有る場合
							stllst = stllst + line2stl(4, vw, d0, d1, d2, d3, d4, d4, p_zb);
							stllst = stllst + line2stl(5, vw, d0, d1, d2, d3, d4, d4, p_zb);
							stllst = stllst + line2stl(7, vw, d0, d1, d2, d3, d4, d4, p_zb);
						}
					}
				} else {
					// 背面ラインは宙に浮かす
					stllst = stllst + line2stl(0, 0, d0, d1, d2, d3, d4, d4, p_zb + d4 * (p_zf - 0.5));
				}
			}
			return stllst;
		}
		private void button1_Click(object sender, EventArgs e) {
			string pfile = text0.Text;
			string sfile = text1.Text;
			string stlall;
			//
			p_zb = 0.0F; flinep = 0; blinep = 0; vlinep = 0;
			for(int i=0; i<5000; i++) fvia[i] = 0;
			stlall = "solid pcb_stl\r\n";
			stlall = stlall + conv_line(pfile);
			stlall = stlall + conv_jtbl();
			stlall = stlall + make_line(1, fline, flinep);
			stlall = stlall + make_line(2, bline, blinep);
			stlall = stlall + "endsolid Cube\r\n";
			//
			text2.Text = stlall;
			File.WriteAllText(sfile, stlall);
		}

		private void set_test(double[,] p) {
			p[0, 0] = 0; p[0, 1] = 0; p[0, 2] = 0; // 0
			p[1, 0] = 20; p[1, 1] = 0; p[1, 2] = 0; // 1
			p[2, 0] = 20; p[2, 1] = 1; p[2, 2] = 0; // 2
			p[3, 0] = 0; p[3, 1] = 1; p[3, 2] = 0; // 3
			p[4, 0] = 0; p[4, 1] = 0; p[4, 2] = 2; // 4
			p[5, 0] = 20; p[5, 1] = 0; p[5, 2] = 2; // 5
			p[6, 0] = 20; p[6, 1] = 1; p[6, 2] = 2; // 6
			p[7, 0] = 0; p[7, 1] = 1; p[7, 2] = 2; // 7
		}
		private void button2_Click(object sender, EventArgs e) {
			double[,] cubep = new double[8, 3];
			set_test(cubep);
			GenerateStl(cubep, out string stltxt);
			text2.Text = stltxt;
		}

	}

}
