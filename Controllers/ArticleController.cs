using Blog.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Blog.Controllers
{
    public class ArticleController : Controller
    {
        // GET: Article
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }
        public ActionResult List()
        {
            using (var database = new BlogDbContext())
            {
                var articles = database.Articles
                    .Include(a => a.Author)
                    .ToList();

                return View(articles);
            }
        }
        public ActionResult Details(int? id)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .First();
                if (article == null)
                {
                    return HttpNotFound();
                }
                return View(article);
            }
        }
        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult Create(Article article, HttpPostedFileBase image)
        {
            if(ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {

                    //Get author id
                    var authorId = database.Users
                        .Where(u => u.UserName == this.User.Identity.Name)
                        .First()
                        .Id;
                    //Set article author
                    if(image != null)
                    {
                        var allowedContentTypes = new[] { "image/jpg", "image/jpeg", "image/png" };
                        if(allowedContentTypes.Contains(image.ContentType))
                        {
                            var imagesPath = "/Content/Images/";
                            var fileName = image.FileName;

                            //this is the related Path (server path for the image)
                            var uploadPath = imagesPath + fileName;
                            //this is the physical Path (your pc path for the image)
                            var pfysicalPath = Server.MapPath(uploadPath);

                            image.SaveAs(pfysicalPath);

                            article.ImagePath = uploadPath;

                        }
                    }
                    article.AuthorId = authorId;
                    //save article to DB
                    database.Articles.Add(article);
                    database.SaveChanges();

                    return RedirectToAction("Index");
                }
            }
            return View(article);
        }
        
        //Get: Article/Delete
        public ActionResult Delete(int? id)
        {
            if( id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var database = new BlogDbContext())
            {
                //Get Article from DataBase
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .First();

                if(! IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }
                //Check if Artilce exists
                if(article == null)
                {
                    return HttpNotFound();
                }
                //Pass article to view
                return View(article);
            }
        }

        //Post: Article/Delete
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(int? id)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            using (var dataBase = new BlogDbContext())
            {
                // Get Aticle from DB
                var article = dataBase.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .First();

                //Check if article exists
                if(article == null)
                {
                    return HttpNotFound();
                }

                //Delete article from dataBase
                dataBase.Articles.Remove(article);
                dataBase.SaveChanges();

                //Redirect to index page
                return RedirectToAction("Index");

            }
        }

        //Get: Article/Edit
        public ActionResult Edit(int? id)
        {
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var dataBase = new BlogDbContext())
            {
                //Get Article from dataBase
                var article = dataBase.Articles
                    .Where(a => a.Id == id)
                    .First();

                if (!IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                //Check if article exists
                if (article == null)
                {
                    return HttpNotFound();
                }

                //Create the view model
                var model = new ArticleViewModel();
                model.id = article.Id;
                model.Title = article.Title;
                model.Content = article.Content;

                //Pass the view model to view
                return View(model);
            }
        }

        //Post: Article/Edit
        public ActionResult Edit(ArticleViewModel model)
        {
            //Check is modle valid
            if (ModelState.IsValid)
            {

                using (var dataBase = new BlogDbContext())
                {
                    //Get article from dataBase
                    var article = dataBase.Articles
                        .FirstOrDefault(a => a.Id == model.id);

                    //Set article properties 
                    article.Title = model.Title;
                    article.Content = model.Content;

                    //Save article state in dataBase
                    dataBase.Entry(article).State = EntityState.Modified;
                    dataBase.SaveChanges();

                    //Redirect to index page
                    return RedirectToAction("Index");
                }
            }
            //if model is invalid return same view
            return View(model);
        }

        private bool IsUserAuthorizedToEdit(Article article)
        {
            bool isAdmin = this.User.IsInRole("Admin");
            bool isAuthor = article.IsAuthor(this.User.Identity.Name);
            return isAdmin || isAuthor;
        }
        
    }
}
