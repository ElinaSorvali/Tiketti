﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TikettiDB.Models;

namespace TikettiDB.Controllers
{

    public class AsiakastiedotController : Controller
    {
        private TikettiDBEntities db = new TikettiDBEntities();

        // GET: Asiakastiedot
        public ActionResult Index()
        {
            if (Session["Sahkoposti"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            int userLevel = (int)Session["Taso"]; // Ota käyttäjän taso istunnosta

            // Tarkista käyttäjän taso ja estä pääsy tietyille sivuille
            if (userLevel == 1)
            {
                // Käyttäjällä on oikeus kaikkiin sivuihin
            }
            else if (userLevel == 2 || userLevel == 3)
            {
                // Käyttäjällä on taso 2 tai 3, estä pääsy haluamillesi sivuille
                ViewBag.ErrorMessage = "Pääsy tälle sivulle vain pääkäyttäjän toimesta!";
                return View("Error"); // Luo virhesivu tai ohjaa käyttäjä virhesivulle
            }

            // Hae Asiakastiedot tietokannasta ja sisällytä Kirjautuminen ja Sijainti
            var asiakastiedot = db.Asiakastiedot
                .Include(a => a.Kirjautuminen)
                .Include(a => a.Sijainti)
                .ToList();

            // Käy läpi jokainen Asiakastiedot-olio
            foreach (var asiakas in asiakastiedot)
            {
                // Hae Sijainti-olion Postinro-arvo
                var postinro = asiakas.Sijainti?.Postinro;

                // Tarkista, että Postinro on määritelty (ei ole null)
                if (postinro != null)
                {
                    // Hae Postinumero-olio tietokannasta, jossa Postinro vastaa nykyisen asiakkaan Postinro:a
                    var postinumero = db.Postinumero
                        .Where(p => p.Postinro == postinro)
                        .FirstOrDefault();

                    // Tarkista, että Postinumero löytyi
                    if (postinumero != null)
                    {
                        // Päivitä asiakkaan Postinumero-ominaisuus tietokannasta löytyneellä tiedolla
                        asiakas.Postinumero = postinumero;
                    }
                }
            }

            // Palauta näkymä Asiakastiedot-listalla, jossa Postinumero-ominaisuudet päivitetty
            return View(asiakastiedot);
        }

        // GET: Asiakastiedot/Create
        public ActionResult Create()
        {
            if (Session["Sahkoposti"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            int userLevel = (int)Session["Taso"]; // Ota käyttäjän taso istunnosta

            // Tarkista käyttäjän taso ja estä pääsy tietyille sivuille
            if (userLevel == 1)
            {
                // Käyttäjällä on oikeus kaikkiin sivuihin
            }
            else if (userLevel == 2 || userLevel == 3)
            {
                // Käyttäjällä on taso 2 tai 3, estä pääsy haluamillesi sivuille
                ViewBag.ErrorMessage = "Pääsy tälle sivulle vain pääkäyttäjän toimesta!";
                return View("Error"); // Luo virhesivu tai ohjaa käyttäjä virhesivulle
            }
            //pudotusvalikon viewbag
            ViewBag.Postinro = new SelectList(db.Postinumero, "Postinro", "Postinro");

            return View();
        }

        public ActionResult _ModalCreate(int? id)
        {
            //pudotusvalikon viewbag
            ViewBag.Postinro = new SelectList(db.Postinumero, "Postinro", "Postinro");

            return PartialView();
        }

        // POST: Asiakastiedot/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Etunimi,Sukunimi,Puhelinnro,Osoite,Postinro,Sahkoposti,Salasana")] Asiakastiedot asiakastiedot)
        {
            if (ModelState.IsValid)
            {
                // Tarkista, että kaikki tarvittavat tiedot ovat annettu
                if (string.IsNullOrEmpty(asiakastiedot.Etunimi) ||
                    string.IsNullOrEmpty(asiakastiedot.Sukunimi) ||
                    string.IsNullOrEmpty(asiakastiedot.Puhelinnro) ||
                    string.IsNullOrEmpty(asiakastiedot.Osoite) ||
                    string.IsNullOrEmpty(asiakastiedot.Postinro) ||
                    string.IsNullOrEmpty(asiakastiedot.Sahkoposti) ||
                    string.IsNullOrEmpty(asiakastiedot.Salasana))
                {
                    ModelState.AddModelError("", "Kaikki tiedot ovat pakollisia.");

                    ViewBag.Postinro = new SelectList(db.Postinumero, "Postinro", "Postinro", asiakastiedot.Postinro);
                    return View(asiakastiedot);
                }

                // Etsi postinro Postinumero-taulusta
                Postinumero postinumero = db.Postinumero.SingleOrDefault(p => p.Postinro == asiakastiedot.Postinro);
                if (postinumero == null)
                {
                    postinumero = new Postinumero
                    {
                        Postinro = asiakastiedot.Postinro
                    };
                    db.Postinumero.Add(postinumero);
                }

                //Kirjautumis-tauluun tiedot
                Kirjautuminen kirjautuminen = db.Kirjautuminen.SingleOrDefault(k => k.Sahkoposti == asiakastiedot.Sahkoposti);
                if (kirjautuminen == null)
                {
                    kirjautuminen = new Kirjautuminen
                    {
                        Sahkoposti = asiakastiedot.Sahkoposti,
                        Salasana = asiakastiedot.Salasana,
                        Taso = 3
                    };
                    db.Kirjautuminen.Add(kirjautuminen);
                }

                // Luo uusi Sijainti-tauluun
                Sijainti sijainti = new Sijainti
                {
                    Osoite = asiakastiedot.Osoite,
                    Postinro = postinumero.Postinro,
                };
                db.Sijainti.Add(sijainti);

                // Luo uusi tieto Asiakastiedot-tauluun
                Asiakastiedot asiakastieto = new Asiakastiedot
                {
                    Etunimi = asiakastiedot.Etunimi,
                    Sukunimi = asiakastiedot.Sukunimi,
                    Puhelinnro = asiakastiedot.Puhelinnro,
                    Osoite = sijainti.Osoite,
                    Postinro = sijainti.Postinro,
                    Sahkoposti = kirjautuminen.Sahkoposti,
                    Salasana = kirjautuminen.Salasana,
                    SijaintiID = sijainti.SijaintiID,
                };

                // Lisää Asiakastiedot tietokantaan
                db.Asiakastiedot.Add(asiakastieto);

                // Tallenna muutokset
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.Postinro = new SelectList(db.Postinumero, "Postinro", "Postinro", asiakastiedot.Postinro);
            //virheenkäsittelyä
            return PartialView(asiakastiedot);
        }

        //OHJELMASSA EI OLE EDITTIÄ
        // GET: Asiakastiedot/Edit/5
        //public ActionResult Edit(int? id)
        //{
        //    if (Session["Sahkoposti"] == null)
        //    {
        //        return RedirectToAction("Login", "Home");
        //    }

        //    int userLevel = (int)Session["Taso"];

        //    if (userLevel == 1)
        //    {
        //        // Käyttäjällä on oikeus kaikkiin sivuihin
        //    }
        //    else if (userLevel == 2 || userLevel == 3)
        //    {
        //        ViewBag.ErrorMessage = "Pääsy tälle sivulle vain pääkäyttäjän toimesta!";
        //        return View("Error");
        //    }

        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    Asiakastiedot asiakastiedot = db.Asiakastiedot.Find(id);

        //    if (asiakastiedot == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    Sijainti sijainti = db.Sijainti.Find(asiakastiedot.SijaintiID);
        //    ViewBag.Osoite = sijainti?.Osoite;

        //    Kirjautuminen kirjautuminen = db.Kirjautuminen.FirstOrDefault(k => k.Sahkoposti == asiakastiedot.Sahkoposti);
        //    ViewBag.Salasana = kirjautuminen?.Salasana;

        //    ViewBag.Postinro = new SelectList(db.Postinumero, "Postinro", "Postinro", asiakastiedot.Postinro);

        //    return View(asiakastiedot);
        //}

        //// POST: Asiakastiedot/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "AsiakasID,SijaintiID,Etunimi,Sukunimi,Puhelinnro,Osoite,Postinro,Sahkoposti")] Asiakastiedot asiakastiedot)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Päivitä Asiakastiedot-tiedot tietokantaan
        //        db.Entry(asiakastiedot).State = EntityState.Modified;

        //        // Päivitä myös Kirjautuminen-tiedot
        //        Kirjautuminen kirjautuminen = db.Kirjautuminen.FirstOrDefault(k => k.Sahkoposti == asiakastiedot.Sahkoposti);
        //        if (kirjautuminen != null)
        //        {
        //            if (kirjautuminen.Sahkoposti != asiakastiedot.Sahkoposti)
        //            {
        //                kirjautuminen.Sahkoposti = asiakastiedot.Sahkoposti;
        //            }
        //        }

        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }

        //    ViewBag.Postinro = new SelectList(db.Postinumero, "Postinro", "Postinro", asiakastiedot.Postinro);
        //    ViewBag.Salasana = db.Kirjautuminen.FirstOrDefault(k => k.Sahkoposti == asiakastiedot.Sahkoposti)?.Salasana;

        //    return View(asiakastiedot);
        //}
        // GET: Asiakastiedot/Delete/5
        public ActionResult Delete(int? id)
        {
            if (Session["Sahkoposti"] == null)
            {
                return RedirectToAction("Login", "Home");
            }
            int userLevel = (int)Session["Taso"]; // Ota käyttäjän taso istunnosta

            // Tarkista käyttäjän taso ja estä pääsy tietyille sivuille
            if (userLevel == 1)
            {
                // Käyttäjällä on oikeus kaikkiin sivuihin
            }
            else if (userLevel == 2 || userLevel == 3)
            {
                // Käyttäjällä on taso 2 tai 3, estä pääsy haluamillesi sivuille
                ViewBag.ErrorMessage = "Pääsy tälle sivulle vain pääkäyttäjän toimesta!";
                return View("Error"); // Luo virhesivu tai ohjaa käyttäjä virhesivulle
            }
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiakastiedot asiakastiedot = db.Asiakastiedot.Find(id);
            if (asiakastiedot == null)
            {
                return HttpNotFound();
            }
            return View(asiakastiedot);
        }

        public ActionResult _ModalDelete(int? id)
        {
            
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asiakastiedot asiakastiedot = db.Asiakastiedot.Find(id);
            if (asiakastiedot == null)
            {
                return HttpNotFound();
            }
            return PartialView(asiakastiedot);
        }

        // POST: Asiakastiedot/Delete/5
        [HttpPost, ActionName("_ModalDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult _ModalDeleteConfirmed(int id)
        {
            Asiakastiedot asiakastiedot = db.Asiakastiedot.Find(id);

            // Poista liittyvät tiedot Kirjautuminen-taulusta
            Kirjautuminen kirjautuminen = db.Kirjautuminen.FirstOrDefault(k => k.Sahkoposti == asiakastiedot.Kirjautuminen.Sahkoposti);
            db.Kirjautuminen.Remove(kirjautuminen);

            // Poista liittyvät tiedot Sijainti-taulusta
            Sijainti sijainti = db.Sijainti.Find(asiakastiedot.SijaintiID);
            db.Sijainti.Remove(sijainti);

            // Poista itse Asiakastiedot
            db.Asiakastiedot.Remove(asiakastiedot);

            db.SaveChanges();

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
