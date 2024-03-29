/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sprixel {
    internal static class Strings {
        
        /// <summary>
        /// A string like  "Method precondition violated"
        /// </summary>
        internal static string MethodPreconditionViolated {
            get {
                return "Method precondition violated";
            }
        }

        /// <summary>
        /// A string like  "Invalid argument value"
        /// </summary>
        internal static string InvalidArgumentValue {
            get {
                return "Invalid argument value";
            }
        }

        /// <summary>
        /// A string like  "Non-empty string required"
        /// </summary>
        internal static string NonEmptyStringRequired {
            get {
                return "Non-empty string required";
            }
        }

        /// <summary>
        /// A string like  "Non-empty collection required"
        /// </summary>
        internal static string NonEmptyCollectionRequired {
            get {
                return "Non-empty collection required";
            }
        }
        
        /// <summary>
        /// A string like  "Invalid delegate type (Invoke method not found)."
        /// </summary>
        internal static string InvalidDelegate {
            get {
                return "Invalid delegate type (Invoke method not found).";
            }
        }
    }

    public static class ContractUtils {

        public static void Requires(bool precondition) {
            if (!precondition) {
                throw new ArgumentException(Strings.MethodPreconditionViolated);
            }
        }

        public static void Requires(bool precondition, string paramName) {
            Assert.NotEmpty(paramName);

            if (!precondition) {
                throw new ArgumentException(Strings.InvalidArgumentValue, paramName);
            }
        }

        public static void Requires(bool precondition, string paramName, string message) {
            Assert.NotEmpty(paramName);

            if (!precondition) {
                throw new ArgumentException(message, paramName);
            }
        }

        public static void RequiresNotNull(object value, string paramName) {
            Assert.NotEmpty(paramName);

            if (value == null) {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void RequiresNotEmpty(string str, string paramName) {
            RequiresNotNull(str, paramName);
            if (str.Length == 0) {
                throw new ArgumentException(Strings.NonEmptyStringRequired, paramName);
            }
        }

        public static void RequiresNotEmpty<T>(ICollection<T> collection, string paramName) {
            RequiresNotNull(collection, paramName);
            if (collection.Count == 0) {
                throw new ArgumentException(Strings.NonEmptyCollectionRequired, paramName);
            }
        }

        /// <summary>
        /// Requires the specified index to point inside the array.
        /// </summary>
        /// <exception cref="ArgumentNullException">Array is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Index is outside the array.</exception>
        public static void RequiresArrayIndex<T>(IList<T> array, int index, string indexName) {
            RequiresArrayIndex(array.Count, index, indexName);
        }

        /// <summary>
        /// Requires the specified index to point inside the array.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Index is outside the array.</exception>
        public static void RequiresArrayIndex(int arraySize, int index, string indexName) {
            Assert.NotEmpty(indexName);
            Debug.Assert(arraySize >= 0);

            if (index < 0 || index >= arraySize) throw new ArgumentOutOfRangeException(indexName);
        }

        /// <summary>
        /// Requires the specified index to point inside the array or at the end
        /// </summary>
        /// <exception cref="ArgumentNullException">Array is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Index is outside the array.</exception>
        public static void RequiresArrayInsertIndex<T>(IList<T> array, int index, string indexName) {
            RequiresArrayInsertIndex(array.Count, index, indexName);
        }

        /// <summary>
        /// Requires the specified index to point inside the array or at the end
        /// </summary>
        /// <exception cref="ArgumentNullException">Array is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Index is outside the array.</exception>
        public static void RequiresArrayInsertIndex(int arraySize, int index, string indexName) {
            Assert.NotEmpty(indexName);
            Debug.Assert(arraySize >= 0);

            if (index < 0 || index > arraySize) throw new ArgumentOutOfRangeException(indexName);
        }

        /// <summary>
        /// Requires the range [offset, offset + count] to be a subset of [0, array.Count].
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Offset or count are out of range.</exception>
        public static void RequiresArrayRange<T>(IList<T> array, int offset, int count, string offsetName, string countName) {
            Assert.NotNull(array);
            RequiresArrayRange(array.Count, offset, count, offsetName, countName);
        }

        /// <summary>
        /// Requires the range [offset, offset + count] to be a subset of [0, array.Count].
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Offset or count are out of range.</exception>
        public static void RequiresArrayRange(int arraySize, int offset, int count, string offsetName, string countName) {
            Assert.NotEmpty(offsetName);
            Assert.NotEmpty(countName);
            Debug.Assert(arraySize >= 0);

            if (count < 0) throw new ArgumentOutOfRangeException(countName);
            if (offset < 0 || arraySize - offset < count) throw new ArgumentOutOfRangeException(offsetName);
        }


        /// <summary>
        /// Requires the range [offset, offset + count] to be a subset of [0, array.Count].
        /// </summary>
        /// <exception cref="ArgumentNullException">Array is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Offset or count are out of range.</exception>
        public static void RequiresListRange(IList array, int offset, int count, string offsetName, string countName) {
            Assert.NotEmpty(offsetName);
            Assert.NotEmpty(countName);
            Assert.NotNull(array);

            if (count < 0) throw new ArgumentOutOfRangeException(countName);
            if (offset < 0 || array.Count - offset < count) throw new ArgumentOutOfRangeException(offsetName);
        }

        /// <summary>
        /// Requires the range [offset, offset + count] to be a subset of [0, array.Count].
        /// </summary>
        /// <exception cref="ArgumentNullException">String is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Offset or count are out of range.</exception>
        public static void RequiresArrayRange(string str, int offset, int count, string offsetName, string countName) {
            Assert.NotEmpty(offsetName);
            Assert.NotEmpty(countName);
            Assert.NotNull(str);

            if (count < 0) throw new ArgumentOutOfRangeException(countName);
            if (offset < 0 || str.Length - offset < count) throw new ArgumentOutOfRangeException(offsetName);
        }

        /// <summary>
        /// Requires the array and all its items to be non-null.
        /// </summary>
        public static void RequiresNotNullItems<T>(IList<T> array, string arrayName) {
            Assert.NotNull(arrayName);
            RequiresNotNull(array, arrayName);

            for (int i = 0; i < array.Count; i++) {
                if (array[i] == null) {
                    throw ExceptionUtils.MakeArgumentItemNullException(i, arrayName);
                }
            }
        }
                
        /// <summary>
        /// Requires the enumerable collection and all its items to be non-null.
        /// </summary>
        public static void RequiresNotNullItems<T>(IEnumerable<T> collection, string collectionName) {
            Assert.NotNull(collectionName);
            RequiresNotNull(collection, collectionName);

            int i = 0;
            foreach (var item in collection) {
                if (item == null) {
                    throw ExceptionUtils.MakeArgumentItemNullException(i, collectionName);
                }
                i++;
            }
        }

        [Conditional("FALSE")]
        public static void Invariant(bool condition) {
            Debug.Assert(condition);
        }

        [Conditional("FALSE")]
        public static void Invariant(bool condition, string message) {
            Debug.Assert(condition, message);
        }

        [Conditional("FALSE")]
        public static void Ensures(bool condition) {
            // nop
        }

        [Conditional("FALSE")]
        public static void Ensures(bool condition, string message) {
            // nop
        }

        public static T Result<T>() { 
            return default(T); 
        }

        public static T Parameter<T>(out T value) { 
            value = default(T); 
            return value; 
        }

        public static T Old<T>(T value) { 
            return value; 
        }
    }
}
