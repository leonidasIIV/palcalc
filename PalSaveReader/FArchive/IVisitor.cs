﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace PalSaveReader.FArchive
{
    // generic visitor
    internal abstract class IVisitor
    {
        public IVisitor(string matchedPath)
        {
            MatchedBasePath = matchedPath;
            MatchedSubPath = null;
        }

        public IVisitor(string basePath, string subPath)
        {
            MatchedBasePath = basePath;
            MatchedSubPath = subPath;
        }

        // must be non-null
        public string MatchedBasePath { get; private set; }

        // may be null
        public virtual string MatchedSubPath { get; protected set; }

        public string MatchedPath
        {
            get
            {
                if (MatchedSubPath == null) return MatchedBasePath;
                else return MatchedBasePath + "." + MatchedSubPath;
            }
        }

        public virtual bool Matches(string path) => MatchedPath == path;

        // Called when the Visitor has reached the end of its matched scope
        public virtual void Exit()
        {

        }

        public virtual void VisitInt(string path, int value) { }
        public virtual void VisitInt64(string path, long value) { }
        public virtual void VisitDouble(string path, int value) { } // ??????????????
        public virtual void VisitFloat(string path, float value) { }
        public virtual void VisitString(string path, string value) { }
        public virtual void VisitBool(string path, bool value) { }
        public virtual void VisitGuid(string path, Guid guid) { }

        public virtual void VisitLiteralProperty(string path, LiteralProperty prop) { }

        public virtual IEnumerable<IVisitor> VisitEnumPropertyBegin(string path, EnumPropertyMeta meta) => Enumerable.Empty<IVisitor>();
        public virtual IEnumerable<IVisitor> VisitStructPropertyBegin(string path, StructPropertyMeta meta) => Enumerable.Empty<IVisitor>();
        public virtual IEnumerable<IVisitor> VisitArrayPropertyBegin(string path, ArrayPropertyMeta meta) => Enumerable.Empty<IVisitor>();
        public virtual IEnumerable<IVisitor> VisitMapPropertyBegin(string path, MapPropertyMeta meta) => Enumerable.Empty<IVisitor>();

        public virtual IEnumerable<IVisitor> VisitCharacterContainerPropertyBegin(string path, CharacterContainerDataPropertyMeta meta) => Enumerable.Empty<IVisitor>();

        // meta is at the end
        public virtual IEnumerable<IVisitor> VisitCharacterPropertyBegin(string path) => Enumerable.Empty<IVisitor>();

        public virtual IEnumerable<IVisitor> VisitArrayEntryBegin(string path, int index, ArrayPropertyMeta meta) => Enumerable.Empty<IVisitor>();
        public virtual IEnumerable<IVisitor> VisitMapEntryBegin(string path, int index, MapPropertyMeta meta) => Enumerable.Empty<IVisitor>();

        public virtual void VisitEnumPropertyEnd(string path, EnumPropertyMeta meta) { }
        public virtual void VisitStructPropertyEnd(string path, StructPropertyMeta meta) { }
        public virtual void VisitArrayPropertyEnd(string path, ArrayPropertyMeta meta) { }
        public virtual void VisitMapPropertyEnd(string path, MapPropertyMeta meta) { }

        public virtual void VisitCharacterContainerPropertyEnd(string path, CharacterContainerDataPropertyMeta meta) { }
        public virtual void VisitCharacterPropertyEnd(string path, CharacterDataPropertyMeta meta) { }

        public virtual void VisitArrayEntryEnd(string path, int index, ArrayPropertyMeta meta) { }
        public virtual void VisitMapEntryEnd(string path, int index, MapPropertyMeta meta) { }
    }

    class ValueCollectingVisitor : IVisitor
    {
        string[] propertiesToCollect;
        Dictionary<string, object> collectedValues = new Dictionary<string, object>();

        public ValueCollectingVisitor(IVisitor parent, params string[] propertySubPaths) : this(parent.MatchedPath, propertySubPaths)
        {
        }

        public ValueCollectingVisitor(string basePath, params string[] propertySubPaths) : base(basePath)
        {
            propertiesToCollect = propertySubPaths;
        }

        public event Action<Dictionary<string, object>> OnExit;

        public override bool Matches(string path) => path.StartsWith(MatchedBasePath);

        public Dictionary<string, object> Result => collectedValues;

        private void VisitValue(string path, object value)
        {
            var propPart = path.Substring(MatchedBasePath.Length);
            if (propertiesToCollect.Length > 0 && !propertiesToCollect.Contains(propPart)) return;

            if (collectedValues.ContainsKey(propPart)) Debugger.Break();
            collectedValues[propPart] = value;
        }

        public override void VisitBool(string path, bool value) => VisitValue(path, value);
        public override void VisitDouble(string path, int value) => VisitValue(path, value);
        public override void VisitFloat(string path, float value) => VisitValue(path, value);
        public override void VisitInt(string path, int value) => VisitValue(path, value);
        public override void VisitGuid(string path, Guid guid) => VisitValue(path, guid);
        public override void VisitInt64(string path, long value) => VisitValue(path, value);
        public override void VisitString(string path, string value) => VisitValue(path, value);

        public override void Exit()
        {
            OnExit?.Invoke(collectedValues);
            OnExit = null;
            collectedValues = null;
        }
    }

    class ValueEmittingVisitor : IVisitor
    {
        string[] propertiesToEmit;

        public ValueEmittingVisitor(IVisitor parent, params string[] propertySubPaths) : this(parent.MatchedPath, propertySubPaths)
        {
        }

        public ValueEmittingVisitor(string basePath, params string[] propertySubPaths) : base(basePath)
        {
            propertiesToEmit = propertySubPaths;
        }

        public event Action<string, object> OnValue;
        public event Action OnExit;

        public override bool Matches(string path) => path.StartsWith(MatchedBasePath);

        private void VisitValue(string path, object value)
        {
            var propPart = path.Substring(MatchedBasePath.Length);
            if (propertiesToEmit.Length > 0 && !propertiesToEmit.Contains(propPart)) return;

            OnValue?.Invoke(propPart, value);
        }

        public override void VisitBool(string path, bool value) => VisitValue(path, value);
        public override void VisitDouble(string path, int value) => VisitValue(path, value);
        public override void VisitFloat(string path, float value) => VisitValue(path, value);
        public override void VisitInt(string path, int value) => VisitValue(path, value);
        public override void VisitGuid(string path, Guid guid) => VisitValue(path, guid);
        public override void VisitInt64(string path, long value) => VisitValue(path, value);
        public override void VisitString(string path, string value) => VisitValue(path, value);

        public override void Exit()
        {
            OnExit?.Invoke();
            OnExit = null;
            OnValue = null;
        }
    }

    //class VisitorCollectingVisitor : IVisitor
    //{

    //}
}
