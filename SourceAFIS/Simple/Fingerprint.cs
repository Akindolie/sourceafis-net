using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Xml.Serialization;
using SourceAFIS.Extraction.Templates;

namespace SourceAFIS.Simple
{
    /// <summary>
    /// Collection of fingerprint-related information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class contains basic information (<see cref="Image"/>, <see cref="Template"/>) about the fingerprint that
    /// is used by SourceAFIS to perform template extraction and fingerprint matching.
    /// If you need to attach application-specific information to <see cref="Fingerprint"/> object,
    /// inherit from this class and add fields as necessary. <see cref="Fingerprint"/> objects can be
    /// grouped in <see cref="Person"/> objects.
    /// </para>
    /// <para>
    /// This class is designed to be easy to serialize in order to be stored in binary format (BLOB)
    /// in application database, binary or XML files, or sent over network. You can either serialize
    /// the whole object or serialize individual properties. You can set some properties to null
    /// to exclude them from serialization.
    /// </para>
    /// </remarks>
    /// <seealso cref="Person"/>
    [Serializable]
    public class Fingerprint : ICloneable
    {
        /// <summary>
        /// Creates empty <see cref="Fingerprint"/> object.
        /// </summary>
        public Fingerprint() { }

        Bitmap ImageValue;
        /// <summary>
        /// Fingerprint image.
        /// </summary>
        /// <value>
        /// Fingerprint image that was used to extract the <see cref="Template"/> or other image
        /// attached later after extraction. This property is null by default.
        /// </value>
        /// <remarks>
        /// <para>
        /// This is the fingerprint image. This property must be set before call to <see cref="AfisEngine.Extract"/>
        /// in order to generate valid <see cref="Template"/>. Once the <see cref="Template"/> is generated, <see cref="Image"/> property has only
        /// informational meaning and it can be set to null to save space. It is however recommended to
        /// keep the original image just in case it is needed to regenerate the <see cref="Template"/> in future.
        /// </para>
        /// <para>
        /// Accessors of this property do not clone the image. To avoid unwanted sharing of the <see cref="Bitmap"/>
        /// object, call <see cref="ICloneable.Clone"/> on the <see cref="Bitmap"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="Template"/>
        /// <seealso cref="AfisEngine.Extract"/>
        [XmlIgnore]
        public Bitmap Image { get { return ImageValue; } set { ImageValue = value; } }

        /// <summary>
        /// Fingerprint template.
        /// </summary>
        /// <value>
        /// Fingerprint template generated by <see cref="AfisEngine.Extract"/> or other template assigned
        /// for example after deserialization. This property is null by default.
        /// </value>
        /// <remarks>
        /// <para>
        /// Fingerprint template is an abstract model of the fingerprint that is serialized
        /// in a very compact binary format (up to a few KB). Templates are better than fingerprint images,
        /// because they require less space and they are easier to match than images. To generate
        /// <see cref="Template"/>, pass <see cref="Fingerprint"/> object with valid <see cref="Image"/> to <see cref="AfisEngine.Extract"/>.
        /// <see cref="Template"/> is required by <see cref="AfisEngine.Verify"/> and <see cref="AfisEngine.Identify"/>.
        /// </para>
        /// <para>
        /// If you need access to the internal structure of the template, have a look at
        /// <see cref="SourceAFIS.Extraction.Templates.SerializedFormat"/> class in SourceAFIS source code.
        /// Format of the template may however change in later versions of SourceAFIS.
        /// Applications are recommended to keep the original <see cref="Image"/> in order to be able
        /// to regenerate the <see cref="Template"/>.
        /// </para>
        /// </remarks>
        /// <seealso cref="Image"/>
        /// <seealso cref="AfisEngine.Extract"/>
        /// <seealso cref="SourceAFIS.Extraction.Templates.SerializedFormat"/>
        [XmlAttribute]
        public byte[] Template
        {
            get { return Decoded != null ? new SerializedFormat().Serialize(Decoded) : null; }
            set { Decoded = value != null ? new SerializedFormat().Deserialize(value) : null; }
        }

        Finger FingerValue;
        /// <summary>
        /// Position of the finger on hand.
        /// </summary>
        /// <value>
        /// Finger (thumb to little) and hand (right or left) that was used to create this fingerprint.
        /// Default value <see cref="F:SourceAFIS.Simple.Finger.Any"/> means unspecified finger position.
        /// </value>
        /// <remarks>
        /// Finger position is used to speed up matching by skipping fingerprint pairs
        /// with incompatible finger positions. Check <see cref="SourceAFIS.Simple.Finger"/> enumeration for information
        /// on how to control this process. Default value <see cref="F:SourceAFIS.Simple.Finger.Any"/> disables this behavior.
        /// </remarks>
        /// <seealso cref="SourceAFIS.Simple.Finger"/>
        [XmlAttribute]
        public Finger Finger { get { return FingerValue; } set { FingerValue = value; } }

        internal Template Decoded;

        /// <summary>
        /// Create deep copy of the <see cref="Fingerprint"/>.
        /// </summary>
        /// <returns>Deep copy of this <see cref="Fingerprint"/>.</returns>
        public Fingerprint Clone()
        {
            Fingerprint clone = new Fingerprint();
            clone.Image = Image != null ? (Bitmap)Image.Clone() : null;
            clone.Template = Template != null ? (byte[])Template.Clone() : null;
            clone.Finger = Finger;
            return clone;
        }

        object ICloneable.Clone() { return Clone(); }
    }
}
