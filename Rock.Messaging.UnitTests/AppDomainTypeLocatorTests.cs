﻿using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Rock.Messaging.Routing;

namespace Rock.Messaging.UnitTests
{
    public class AppDomainTypeLocatorTests
    {
        protected AppDomainTypeLocator _locator;

        [SetUp]
        public void Setup()
        {
            _locator = new AppDomainTypeLocator(new XmlMessageParser());
        }

        public class TheGetMessageTypeMethod : AppDomainTypeLocatorTests
        {
            [Test]
            public void ReturnsTheTypeThatMatchesTheTypeName()
            {
                var result = _locator.GetMessageType(typeof(FooCommand1).Name);

                Assert.That(result, Is.EqualTo(typeof(FooCommand1)));
            }

            [Test]
            public void WillNotReturnAnAbstractType()
            {
                Assert.That(() => _locator.GetMessageType(typeof(BarEvent1).Name), Throws.Exception);
            }

            [Test]
            public void WillNotReturnATypeThatDoesNotImplementIMessage()
            {
                Assert.That(() => _locator.GetMessageType(typeof(NotAMessage1).Name), Throws.Exception);
            }

            [Test]
            public void ThrowsAnExceptionIfMoreThanOneTypeIsFound()
            {
                Assert.That(() => _locator.GetMessageType("BazMessage1"), Throws.Exception);
            }

            [Test]
            public void ThrowsAnExceptionIfNoTypesAreFound()
            {
                Assert.That(() => _locator.GetMessageType("ThisTypeDoesNotExist"), Throws.Exception);
            }
        }

        public class TheGetMessageHandlerTypeMethod : AppDomainTypeLocatorTests
        {
            [Test]
            public void ReturnsATypeThatImplementIMessageHandlerOfTheSpecifiedType()
            {
                var result = _locator.GetMessageHandlerType(typeof(FooCommand1));

                Assert.That(result, Is.EqualTo(typeof(FooCommand1Handler)));
            }

            [Test]
            public void WillNotReturnAnAbstractType()
            {
                Assert.That(() => _locator.GetMessageHandlerType(typeof(FooCommand2)), Throws.Exception);
            }

            [Test]
            public void ThrowsAnExceptionIfMoreThanOneTypeIsFound()
            {
                Assert.That(() => _locator.GetMessageHandlerType(typeof(FooCommand3)), Throws.Exception);
            }

            [Test]
            public void ThrowsAnExceptionIfNoTypesAreFound()
            {
                Assert.That(() => _locator.GetMessageHandlerType(typeof(FooCommand4)), Throws.Exception);
            }
        }

        public class FooCommand1 : IMessage
        {
        }

        public class FooCommand2 : IMessage
        {
        }

        public class FooCommand3 : IMessage
        {
        }

        public class FooCommand4 : IMessage
        {
        }

        public abstract class BarEvent1 : IMessage
        {
        }

        public class Wrapper1
        {
            public class BazMessage1 : IMessage
            {
            }
        }

        public class Wrapper2
        {
            public class BazMessage1 : IMessage
            {
            }
        }

        public class NotAMessage1
        {
        }

        public class FooCommand1Handler : IMessageHandler<FooCommand1>
        {
            public Task<FooCommand1> Handle(FooCommand1 message)
            {
                throw new NotImplementedException();
            }
        }

        public abstract class FooCommand2Handler : IMessageHandler<FooCommand2>
        {
            public Task<FooCommand2> Handle(FooCommand2 message)
            {
                throw new NotImplementedException();
            }
        }

        public class FooCommand3Handler1 : IMessageHandler<FooCommand3>
        {
            public Task<FooCommand3> Handle(FooCommand3 message)
            {
                throw new NotImplementedException();
            }
        }

        public class FooCommand3Handler2 : IMessageHandler<FooCommand3>
        {
            public Task<FooCommand3> Handle(FooCommand3 message)
            {
                throw new NotImplementedException();
            }
        }
    }
}