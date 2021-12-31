using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using QLKS.Models;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;

namespace QLKS.Controllers
{
    public class HomeController : Controller
    {
        private dataQLKSEntities db = new dataQLKSEntities();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        [HttpPost]
        public ActionResult Contact(String ho_ten, String mail, String noi_dung)
        {
            if (noi_dung == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            TBLKHACHHANG kh = (TBLKHACHHANG)Session["KH"];
            if (kh == null)
            {
                if (ho_ten == null || mail == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (noi_dung.Length >= 500)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            TBLTINNHAN tn = new TBLTINNHAN();
            if (kh == null)
            {
                tn.ID = "";
                tn.HO_TEN = ho_ten;
                tn.MAIL = mail;
            }
            else
            {
                tn.MA_KH = kh.MA_KH;
            }
            tn.NOI_DUNG = noi_dung;
            tn.NGAY_GUI = DateTime.Now;
            try
            {
                db.TBLTINNHANs.Add(tn);
                db.SaveChanges();
                ModelState.AddModelError("", "Gửi ticket thành công !");
            }
            catch
            {
                ModelState.AddModelError("", "Có lỗi xảy ra!");
            }
            return View();
        }

        [HttpGet]
        public ActionResult FindRoom()
        {
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public ActionResult FindRoom(String datestart, String dateend)
        {
            List<TBLPHONG> li = new List<TBLPHONG>();
            if (datestart.Equals("") || dateend.Equals(""))
            {
                li = db.TBLPHONGs.ToList();
            }
            else
            {
                Session["ds_ma_phong"] = null;
                Session["ngay_vao"] = datestart;
                Session["ngay_ra"] = dateend;

                datestart = DateTime.ParseExact(datestart, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy/MM/dd");
                dateend = DateTime.ParseExact(dateend, "dd/MM/yyyy", CultureInfo.InvariantCulture).ToString("yyyy/MM/dd");

                DateTime dateS = (DateTime.Parse(datestart)).AddHours(12);
                DateTime dateE = (DateTime.Parse(dateend)).AddHours(12);
                li = db.TBLPHONGs.Where(t => !(db.TBLPHIEUDATPHONGs.Where(m => (m.MA_TINH_TRANG == 1 || m.MA_TINH_TRANG == 2)
                    && m.NGAY_RA > dateS && m.NGAY_VAO < dateE))
                    .Select(m => m.MA_PHONG).ToList().Contains(t.MA_PHONG)).ToList();
            }
            return View(li);
        }
        public ActionResult ChonPhong(string id)
        {
            //Session["ma_phong"] = id;
            try
            {
                List<int> ds;
                ds = (List<int>)Session["ds_ma_phong"];
                if (ds == null)
                    ds = new List<int>();
                ds.Add(Int32.Parse(id));
                Session["ds_ma_phong"] = ds;
                ViewBag.result = "success";
            }
            catch
            {
                ViewBag.result = "error";
            }
            return View();
            //return RedirectToAction("BookRoom", "Home");
        }
        public ActionResult HuyChon(string id)
        {
            try
            {
                List<int> ds;
                ds = (List<int>)Session["ds_ma_phong"];
                if (ds == null)
                    ds = new List<int>();
                ds.Remove(Int32.Parse(id));
                Session["ds_ma_phong"] = ds;
                ViewBag.result = "success";

            }
            catch
            {
                ViewBag.result = "error";
            }
            return View();
        }
        public ActionResult BookRoom()
        {
            if (Session["KH"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            AutoHuyPhieuDatPhong();
            TBLKHACHHANG kh = (TBLKHACHHANG)Session["KH"];
            ViewBag.ma_kh = kh.MA_KH;
            ViewBag.ten_kh = kh.HO_TEN;
            ViewBag.ngay_dat = DateTime.Now;
            ViewBag.ngay_vao = (String)Session["ngay_vao"];
            ViewBag.ngay_ra = (String)Session["ngay_ra"];

            //if (Session["ma_phong"] != null)
            //{
            //    ViewBag.ma_phong = (String)Session["ma_phong"];
            //    int map = Int32.Parse((String)Session["ma_phong"]);
            //    tblPhong p = (tblPhong)db.tblPhongs.Find(map);
            //    ViewBag.so_phong = p.so_phong;
            //}
            String sp = "";
            List<int> ds;
            ds = (List<int>)Session["ds_ma_phong"];
            if (ds == null)
                ds = new List<int>();
            ViewBag.ma_phong = JsonConvert.SerializeObject(ds);
            foreach (var item in ds)
            {
                TBLPHONG p = (TBLPHONG)db.TBLPHONGs.Find(item);
                sp += p.SO_PHONG.ToString() + ", ";
            }
            ViewBag.so_phong = sp;
            var liP = db.TBLPHIEUDATPHONGs.Where(u => u.MA_KH == kh.MA_KH && u.MA_TINH_TRANG == 1).ToList();
            return View(liP);
        }
        private void AutoHuyPhieuDatPhong()
        {
            var datenow = DateTime.Now;
            var tblPhieuDatPhongs = db.TBLPHIEUDATPHONGs.Where(u => u.MA_TINH_TRANG == 1).Include(t => t.TBLKHACHHANG).Include(t => t.TBLPHONG).Include(t => t.TBLTINHTRANGPHIEUDATPHONG).ToList();
            foreach (var item in tblPhieuDatPhongs)
            {
                System.Diagnostics.Debug.WriteLine((item.NGAY_VAO - datenow).Value.Days);
                if ((item.NGAY_VAO - datenow).Value.Days < 0)
                {
                    item.MA_TINH_TRANG = 3;
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }
        public ActionResult Result(String ma_kh, String ngay_vao, String ngay_ra, String ma_phong)
        {
            if (ma_kh == null || ngay_vao == null || ngay_ra == null || ma_phong == null)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                TBLPHIEUDATPHONG tgd = new TBLPHIEUDATPHONG();
                List<int> ds = JsonConvert.DeserializeObject<List<int>>(ma_phong);
                tgd.MA_PDP = "";
                tgd.MA_KH = ma_kh;
                tgd.MA_TINH_TRANG = 1;
                tgd.NGAY_DAT = DateTime.Now;
                tgd.NGAY_VAO = (DateTime.ParseExact(ngay_vao, "dd/MM/yyyy", CultureInfo.InvariantCulture)).AddHours(12);
                tgd.NGAY_RA = (DateTime.ParseExact(ngay_ra, "dd/MM/yyyy", CultureInfo.InvariantCulture)).AddHours(12);
                //try
                //{
                for (int i = 0; i < ds.Count; i++)
                {
                    tgd.MA_PHONG = ds[i];   
                    db.TBLPHIEUDATPHONGs.Add(tgd);
                    db.SaveChanges();
                    ViewBag.Result = "success";
                }
                ViewBag.ngay_vao = tgd.NGAY_VAO;
                setNull();
                //}
                //catch
                //{
                //    ViewBag.Result = "error";
                //}
            }
            return View();
        }

        public ActionResult HuyPhieuDatPhong()
        {
            setNull();
            return RedirectToAction("BookRoom", "Home");
        }
        private void setNull()
        {
            Session["ds_ma_phong"] = null;
            Session["ngay_vao"] = null;
            Session["ngay_ra"] = null;
            Session["ma_phong"] = null;
        }
        public ActionResult Chat()
        {
            return View();
        }
        public ActionResult Upload()
        {
            return View();
        }
        public ActionResult Slider(string idlp)
        {
            if (idlp == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //tblPhong p = db.tblPhongs.Include(a => a.tblLoaiPhong).Where(a=>a.ma_phong==id).First();
            TBLLOAIPHONG lp = db.TBLLOAIPHONGs.Find(idlp);
            return View(lp);
        }
        public ActionResult SMS(String ho_ten, String mail, String noi_dung)
        {
            if (noi_dung == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            TBLKHACHHANG kh = (TBLKHACHHANG)Session["KH"];
            if (kh == null)
            {
                if (ho_ten == null || mail == null)
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (noi_dung.Length >= 500)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            TBLTINNHAN tn = new TBLTINNHAN();
            if (kh == null)
            {
                tn.HO_TEN = ho_ten;
                tn.MAIL = mail;
            }
            else
            {
                tn.MA_KH = kh.MA_KH;
            }
            tn.NOI_DUNG = noi_dung;
            try
            {
                db.TBLTINNHANs.Add(tn);
                db.SaveChanges();
                ViewBag.result = "success";
            }
            catch
            {
                ViewBag.result = "error";
            }
            return View();
        }
    }
}