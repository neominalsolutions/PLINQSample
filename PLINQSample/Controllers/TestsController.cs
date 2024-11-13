using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLINQSample.Models;
using System.Collections.Concurrent;

namespace PLINQSample.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class TestsController : ControllerBase
  {
    private readonly NorthwndContext db = new NorthwndContext();

    public TestsController()
    {

    }


    [HttpGet("asParalel")]
    public IActionResult AsParalel()
    {
      // ParallelExecutionMode.ForceParallelism paralel çalışmaya zorla
      // Bazen iş yüküne göre birden fazla thread açmayabilir bu durumda paralel olarak iş parçacığına zorlamak için.

      // Paralel işlem yapar ama UI Thread Blocklar.

      ConcurrentBag<Category> clist = new(); // 5 Milyonluk Kayıt var.

      // bu arkadaş sıralı bir listeye dönüş.
      // clist.Where(x => x.CategoryName.Contains("c")).AsParallel().AsSequential().ToList();

      // bu kaydın paralelde ram üzerinde yönetimini sağlar.
      //clist.Where(x => x.CategoryName.Contains("c")).AsParallel().AsOrdered().ForAll(item =>
      //{

      //});

      // unordered olarak çalışır
      Enumerable.Range(0, 20000).AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).ForAll(x =>
      {
        Console.WriteLine("Thread Paralel: " + Thread.CurrentThread.ManagedThreadId);
        Console.WriteLine("Number:" + x);

      });

      //Enumerable.Range(0, 20000).Where(x => x % 2 == 0).AsParallel().AsOrdered().ForAll(x =>
      //{
      //  Console.WriteLine("Thread Paralel: " + Thread.CurrentThread.ManagedThreadId);
      //  Console.WriteLine("Number:" + x);

      //});


      // AsSequential paralel bir ifadenin sıralı IEnumerable tipine dönüşmesi var
      //Enumerable.Range(0, 20000).Where(x => x % 2 == 0).AsParallel().AsSequential().ToList().ForEach(x =>
      //{
      //  Console.WriteLine("Thread Paralel: " + Thread.CurrentThread.ManagedThreadId);
      //  Console.WriteLine("Number:" + x);

      //});

      // Senkron
      //Enumerable.Range(0, 20000).ToList().ForEach(x =>
      //{
      //  Console.WriteLine("Thread Sync: " + Thread.CurrentThread.ManagedThreadId);
      //  Console.WriteLine("Number:" + x);
      //});

      return Ok();
    }

    [HttpGet("asParalelAsync")]
    public IActionResult AsParalelAsync()
    {
      // ParallelExecutionMode.ForceParallelism paralel çalışmaya zorla
      // Bazen iş yüküne göre birden fazla thread açmayabilir bu durumda paralel olarak iş parçacığına zorlamak için.

      // Paralel işlem yapar ama UI Thread Blocklar.

      Task.Run(() =>
      {
        Enumerable.Range(0, 20000).AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).ForAll(x =>
        {
          Console.WriteLine("Thread Paralel: " + Thread.CurrentThread.ManagedThreadId);
          Console.WriteLine("Number:" + x);

        });

      });

      return Ok();
    }

    [HttpGet("asOrdered")]
    public IActionResult AsOrdered()
    {
      // AsOrdered() performansı düşürür fakat listeyi sıralı olarak verir.
      // WithDegreeOfParallelism Bu iş parçacığı için paralelde 
      var paralelQuery = Enumerable.Range(0, 10).AsParallel().AsOrdered<int>();

      foreach (var item in paralelQuery)
      {
        Console.WriteLine("Number:" + item);
      }


      return Ok();
    }

    [HttpGet("WithDegreeOfParallelism")]
    public IActionResult WithDegreeOfParallelism()
    {
      // AsOrdered() performansı düşürür fakat listeyi sıralı olarak verir.
      // WithDegreeOfParallelism: Paralel iş parçacığı sayısını kontrol etmenizi sağlar.
      // WithDegreeOfParallelism kayıt sayısından eminseniz 15000 kayıt.

      Enumerable.Range(0, 30).AsParallel().WithDegreeOfParallelism(4).ForAll(x =>
      {
        Console.WriteLine("Thread Paralel: " + Thread.CurrentThread.ManagedThreadId);
        Console.WriteLine("Number:" + x);
      });




      return Ok();
    }

    [HttpGet("asParalelQuery")]
    public IActionResult AsParalelQuery()
    {
      var entities = db.Products.Include(x => x.Category).Include(x => x.Supplier);

      // 1. yöntem daha doğru bir yazım.
      entities.Where(x => x.Category.CategoryName.Contains("Bev")).AsParallel().ForAll(x =>
      {
        Console.WriteLine($"Ürün: {x.ProductName}  Category: {x.Category.CategoryName}");

      });

      // Yanlış Sorgu Where olmadan sorgu gider.
      // Not: AsParallel().Where kullandığımızda veri tabanına where düşmeyip sadece program tarafında veritabanından çekilen bilgiyi koşulla göre filtrelemiş oluyoruz.
     
      //entities.AsParallel().Where(x => x.Category.CategoryName.Contains("Bev")).ForAll(x =>
      //{
      //  Console.WriteLine($"Ürün: {x.ProductName}  Category: {x.Category.CategoryName}");

      //});



      return Ok();
    }


    [HttpGet("asParalelException")]
    public IActionResult asParalelException()
    {

      try
      {
        Enumerable.Range(0, 10).AsParallel().Where(x => x / 0 == 0).ForAll(x =>
        {

          Console.WriteLine($"x : {x}");
        });

      }
      catch (AggregateException ex) //  Birden fazla hata veya istisna durumunu temsil eden sınıfımız ile hataları yakalarız.
      {

        ex.InnerExceptions.ToList().ForEach(a =>
        {
          Console.WriteLine(a.Message);
        });
      }

      return Ok();
    }


    [HttpGet("withCancelationToken")]
    public IActionResult withCancelationToken(CancellationToken token)
    {
      try
      {
        Enumerable.Range(0, 10000).AsParallel().Where(x =>
        {
          Thread.SpinWait(50000); // Her bir Item için 5000 satır döngüyü simüle et
          return true;

        }).WithCancellation(token).ForAll(x =>
        {
          Thread.SpinWait(50000); 
          Console.WriteLine($"x : {x}");
        });
      }
      catch (OperationCanceledException ex)
      {
        Console.WriteLine("Operasyon Iptal edildi");
      }

      return Ok();
    }

  }
}
