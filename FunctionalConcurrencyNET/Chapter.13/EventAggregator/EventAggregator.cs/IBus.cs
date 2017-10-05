using System;

namespace RxBus {
  // Message bus used to distribute messages.
  public interface IBus : IObservable<object>, IDisposable {

    void AddPublisher(IObservable<object> observable);
  }
}