﻿using System;
using System.Collections.Generic;
using System.Net.Mime;

namespace RockLib.Messaging.CloudEvents
{
    /// <summary>
    /// The base type for cloud events.
    /// </summary>
    public abstract class CloudEvent
    {
        /// <summary>The name of the <see cref="Id"/> attribute.</summary>
        public const string IdAttribute = "id";

        /// <summary>The name of the <see cref="Source"/> attribute.</summary>
        public const string SourceAttribute = "source";

        /// <summary>The name of the <see cref="SpecVersion"/> attribute.</summary>
        public const string SpecVersionAttribute = "specversion";

        /// <summary>The name of the <see cref="Type"/> attribute.</summary>
        public const string TypeAttribute = "type";

        /// <summary>The name of the <see cref="DataContentType"/> attribute.</summary>
        public const string DataContentTypeAttribute = "datacontenttype";

        /// <summary>The name of the <see cref="DataSchema"/> attribute.</summary>
        public const string DataSchemaAttribute = "dataschema";

        /// <summary>The name of the <see cref="Subject"/> attribute.</summary>
        public const string SubjectAttribute = "subject";

        /// <summary>The name of the <see cref="Time"/> attribute.</summary>
        public const string TimeAttribute = "time";

        private static IProtocolBinding _defaultProtocolBinding = ProtocolBinding.Default;

        /// <summary>
        /// Gets or sets the default <see cref="IProtocolBinding"/>. This is used when one a
        /// <see cref="IProtocolBinding"/> is required by a method but was not provided
        /// (i.e. passed as <see langword="null"/>) by the caller.
        /// </summary>
        public static IProtocolBinding DefaultProtocolBinding
        {
            get => _defaultProtocolBinding;
            set => _defaultProtocolBinding = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// REQUIRED. Identifies the event. Producers MUST ensure that source + id is unique for each
        /// distinct event. If a duplicate event is re-sent (e.g. due to a network error) it MAY have
        /// the same id. Consumers MAY assume that Events with identical source and id are duplicates.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// REQUIRED. Identifies the context in which an event happened. Often this will include
        /// information such as the type of the event source, the organization publishing the event
        /// or the process that produced the event. The exact syntax and semantics behind the data
        /// encoded in the URI is defined by the event producer.
        /// </summary>
        public Uri Source { get; set; }

        /// <summary>
        /// The version of the CloudEvents specification which the event uses. This enables
        /// the interpretation of the context. Compliant event producers MUST use a value of 1.x-wip
        /// when referring to this version of the specification.
        /// </summary>
        public string SpecVersion => "1.0";

        /// <summary>
        /// REQUIRED. This attribute contains a value describing the type of event related to the
        /// originating occurrence. Often this attribute is used for routing, observability, policy
        /// enforcement, etc. The format of this is producer defined and might include information
        /// such as the version of the type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Content type of data value.
        /// </summary>
        public ContentType DataContentType { get; set; }

        /// <summary>
        /// Identifies the schema that data adheres to. Incompatible changes to the schema SHOULD be
        /// reflected by a different URI.
        /// </summary>
        public Uri DataSchema { get; set; }

        /// <summary>
        /// This describes the subject of the event in the context of the event producer (identified
        /// by source). In publish-subscribe scenarios, a subscriber will typically subscribe to events
        /// emitted by a source, but the source identifier alone might not be sufficient as a qualifier
        /// for any specific event if the source context has internal sub-structure.
        /// 
        /// <para>Identifying the subject of the event in context metadata (opposed to only in the data
        /// payload) is particularly helpful in generic subscription filtering scenarios where middleware
        /// is unable to interpret the data content.In the above example, the subscriber might only be
        /// interested in blobs with names ending with '.jpg' or '.jpeg' and the subject attribute allows
        /// for constructing a simple and efficient string-suffix filter for that subset of events.</para>
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Timestamp of when the occurrence happened.
        /// </summary>
        public DateTime? Time { get; set; }

        /// <summary>
        /// Any additional attributes not specific to this CloudEvent type.
        /// </summary>
        public IDictionary<string, object> AdditionalAttributes { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Domain-specific information about the occurrence (i.e. the payload). This might include
        /// information about the occurrence, details about the data that was changed, or more.
        /// </summary>
        public object Data { get; private set; }

        /// <summary>
        /// Sets the data of the cloud event.
        /// </summary>
        /// <param name="data">The data of the cloud event.</param>
        public void SetData(string data) => Data = data;

        /// <summary>
        /// Sets the data of the cloud event.
        /// </summary>
        /// <param name="data">The data of the cloud event.</param>
        public void SetData(byte[] data) => Data = data;

        /// <summary>
        /// Creates a <see cref="SenderMessage"/> with headers mapped from the attributes of this cloud event.
        /// </summary>
        /// <param name="protocolBinding">
        /// The <see cref="IProtocolBinding"/> used to map CloudEvent attributes to <see cref="SenderMessage"/>
        /// headers. If <see langword="null"/>, then <see cref="DefaultProtocolBinding"/> is used instead.
        /// </param>
        /// <returns>The mapped <see cref="SenderMessage"/>.</returns>
        public virtual SenderMessage ToSenderMessage(IProtocolBinding protocolBinding = null)
        {
            if (protocolBinding is null)
                protocolBinding = DefaultProtocolBinding;

            SenderMessage senderMessage;

            if (Data is string stringData)
                senderMessage = new SenderMessage(stringData);
            else if (Data is byte[] binaryData)
                senderMessage = new SenderMessage(binaryData);
            else
                senderMessage = new SenderMessage("");

            if (Id != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(IdAttribute)] = Id;

            if (Source != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(SourceAttribute)] = Source;

            senderMessage.Headers[protocolBinding.GetHeaderName(SpecVersionAttribute)] = SpecVersion;

            if (Type != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(TypeAttribute)] = Type;

            if (DataContentType != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(DataContentTypeAttribute)] = DataContentType;

            if (DataSchema != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(DataSchemaAttribute)] = DataSchema;

            if (Subject != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(SubjectAttribute)] = Subject;

            if (Time != null)
                senderMessage.Headers[protocolBinding.GetHeaderName(TimeAttribute)] = Time.Value;

            foreach (var attribute in AdditionalAttributes)
                senderMessage.Headers[attribute.Key] = attribute.Value;

            return senderMessage;
        }

        /// <summary>
        /// Ensures that the required base cloud event attributes are present.
        /// </summary>
        /// <param name="senderMessage">The <see cref="SenderMessage"/> to validate.</param>
        /// <param name="protocolBinding">
        /// The <see cref="IProtocolBinding"/> used to map CloudEvent attributes to <see cref="SenderMessage"/>
        /// headers. If <see langword="null"/>, then <see cref="DefaultProtocolBinding"/> is used instead.
        /// </param>
        protected static void ValidateCore(SenderMessage senderMessage, IProtocolBinding protocolBinding)
        {
            if (senderMessage is null)
                throw new ArgumentNullException(nameof(senderMessage));

            if (protocolBinding is null)
                protocolBinding = DefaultProtocolBinding;

            if (!TryGetHeaderValue<string>(senderMessage, protocolBinding.GetHeaderName(IdAttribute), out _))
                senderMessage.Headers[protocolBinding.GetHeaderName(IdAttribute)] = Guid.NewGuid().ToString();

            if (!TryGetHeaderValue<Uri>(senderMessage, protocolBinding.GetHeaderName(SourceAttribute), out _)
                && !TryGetHeaderValue<string>(senderMessage, protocolBinding.GetHeaderName(SourceAttribute), out _))
                throw new CloudEventValidationException($"The '{protocolBinding.GetHeaderName(SourceAttribute)}' header is missing from the SenderMessage.");

            if (!TryGetHeaderValue<string>(senderMessage, protocolBinding.GetHeaderName(TypeAttribute), out _))
                throw new CloudEventValidationException($"The '{protocolBinding.GetHeaderName(TypeAttribute)}' header is missing from the SenderMessage.");

            if (!TryGetHeaderValue<DateTime>(senderMessage, protocolBinding.GetHeaderName(TimeAttribute), out _))
                senderMessage.Headers[protocolBinding.GetHeaderName(TimeAttribute)] = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates an instance of <typeparamref name="TCloudEvent"/> and initializes its base
        /// cloud event attributes according to the payload and headers of the
        /// <paramref name="receiverMessage"/>.
        /// </summary>
        /// <typeparam name="TCloudEvent">The type of cloud event to create.</typeparam>
        /// <param name="receiverMessage">
        /// The <see cref="IReceiverMessage"/> with headers that map to cloud event attributes.
        /// </param>
        /// <param name="protocolBinding">
        /// The <see cref="IProtocolBinding"/> used to map <see cref="IReceiverMessage"/> headers to
        /// CloudEvent attributes. If <see langword="null"/>, then <see cref="DefaultProtocolBinding"/>
        /// is used instead.
        /// </param>
        /// <returns>
        /// A new instance of <typeparamref name="TCloudEvent"/> with its base cloud event attributes set.
        /// </returns>
        protected static TCloudEvent CreateCore<TCloudEvent>(IReceiverMessage receiverMessage, IProtocolBinding protocolBinding)
            where TCloudEvent : CloudEvent, new()
        {
            if (receiverMessage is null)
                throw new ArgumentNullException(nameof(receiverMessage));

            if (protocolBinding is null)
                protocolBinding = DefaultProtocolBinding;

            var cloudEvent = new TCloudEvent();
            var additionalAttributes = cloudEvent.AdditionalAttributes;

            foreach (var header in receiverMessage.Headers)
                additionalAttributes.Add(header);

            if (receiverMessage.IsBinary())
                cloudEvent.SetData(receiverMessage.BinaryPayload);
            else
                cloudEvent.SetData(receiverMessage.StringPayload);

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(IdAttribute), out string id))
            {
                cloudEvent.Id = id;
                additionalAttributes.Remove(protocolBinding.GetHeaderName(IdAttribute));
            }

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(SourceAttribute), out Uri source))
            {
                cloudEvent.Source = source;
                additionalAttributes.Remove(protocolBinding.GetHeaderName(SourceAttribute));
            }

            // SpecVersion?

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(TypeAttribute), out string type))
            {
                cloudEvent.Type = type;
                additionalAttributes.Remove(protocolBinding.GetHeaderName(TypeAttribute));
            }

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(DataContentTypeAttribute), out ContentType dataContentType))
            {
                cloudEvent.DataContentType = dataContentType;
                additionalAttributes.Remove(protocolBinding.GetHeaderName(DataContentTypeAttribute));
            }
            else if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(DataContentTypeAttribute), out string dataContentTypeString))
            {
                cloudEvent.DataContentType = new ContentType(dataContentTypeString);
                additionalAttributes.Remove(protocolBinding.GetHeaderName(DataContentTypeAttribute));
            }

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(DataSchemaAttribute), out Uri dataSchema))
            {
                cloudEvent.DataSchema = dataSchema;
                additionalAttributes.Remove(protocolBinding.GetHeaderName(DataSchemaAttribute));
            }

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(SubjectAttribute), out string subject))
            {
                cloudEvent.Subject = subject;
                additionalAttributes.Remove(protocolBinding.GetHeaderName(SubjectAttribute));
            }

            if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(TimeAttribute), out DateTime time))
            {
                cloudEvent.Time = time;
                additionalAttributes.Remove(protocolBinding.GetHeaderName(TimeAttribute));
            }
            else if (receiverMessage.Headers.TryGetValue(protocolBinding.GetHeaderName(TimeAttribute), out string timeString))
            {
                cloudEvent.Time = DateTime.Parse(timeString);
                additionalAttributes.Remove(protocolBinding.GetHeaderName(TimeAttribute));
            }

            return cloudEvent;
        }

        /// <summary>
        /// Gets the value of the header with the specified name as type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the header value.</typeparam>
        /// <param name="senderMessage">The <see cref="SenderMessage"/>.</param>
        /// <param name="headerName">The name of the header.</param>
        /// <param name="value">
        /// When this method returns, the value of the header with the specified name, if the
        /// header is found; otherwise, the default value for the type of the <paramref name="value"/>
        /// parameter. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the message contains a header with the specified name; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        protected static bool TryGetHeaderValue<T>(SenderMessage senderMessage, string headerName, out T value)
        {
            if (senderMessage.Headers.TryGetValue(headerName, out var obj) && obj is T)
            {
                // TODO: If obj is not of type T, try to convert it to T.
                value = (T)obj;
                return true;
            }
            value = default;
            return false;
        }
    }
}
