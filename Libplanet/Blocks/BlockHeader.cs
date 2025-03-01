using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;
using Libplanet.Crypto;

namespace Libplanet.Blocks
{
    /// <summary>
    /// Block header containing information about <see cref="Block{T}"/>s except transactions.
    /// </summary>
    public sealed class BlockHeader : IBlockHeader
    {
        private readonly PreEvaluationBlockHeader _preEvaluationBlockHeader;

        /// <summary>
        /// Creates a <see cref="BlockHeader"/> instance from the given
        /// <paramref name="preEvaluationBlockHeader"/> and <paramref name="stateRootHash"/>.
        /// This automatically derives its hash from the given inputs.
        /// </summary>
        /// <param name="preEvaluationBlockHeader">The pre-evaluation block header.</param>
        /// <param name="stateRootHash">The state root hash.</param>
        /// <param name="signature">The block signature.</param>
        /// <exception cref="InvalidBlockSignatureException">Thrown when
        /// the <paramref name="signature"/> signature is invalid.</exception>
        public BlockHeader(
            PreEvaluationBlockHeader preEvaluationBlockHeader,
            HashDigest<SHA256> stateRootHash,
            ImmutableArray<byte>? signature
        )
#pragma warning disable SA1118
            : this(
                preEvaluationBlockHeader,
                (
                    stateRootHash,
                    signature,
                    preEvaluationBlockHeader.DeriveBlockHash(stateRootHash, signature)
                )
            )
#pragma warning restore SA1118
        {
        }

        /// <summary>
        /// Creates a <see cref="BlockHeader"/> instance from the given
        /// <paramref name="preEvaluationBlockHeader"/> and <paramref name="stateRootHash"/>.
        /// It also checks the sanity of the given <paramref name="hash"/>.
        /// </summary>
        /// <param name="preEvaluationBlockHeader">The pre-evaluation block header.</param>
        /// <param name="stateRootHash">The state root hash.</param>
        /// <param name="signature">The block signature.</param>
        /// <param name="hash">The block hash to check.</param>
        /// <exception cref="InvalidBlockSignatureException">Thrown when
        /// the <paramref name="signature"/> signature is invalid.</exception>
        /// <exception cref="InvalidBlockHashException">Thrown when the given block
        /// <paramref name="hash"/> is consistent with other arguments.</exception>
        public BlockHeader(
            PreEvaluationBlockHeader preEvaluationBlockHeader,
            HashDigest<SHA256> stateRootHash,
            ImmutableArray<byte>? signature,
            BlockHash hash
        )
            : this(
                preEvaluationBlockHeader,
                (stateRootHash, signature, hash)
            )
        {
            BlockHash expectedHash =
                preEvaluationBlockHeader.DeriveBlockHash(stateRootHash, signature);
            if (!hash.Equals(expectedHash))
            {
                throw new InvalidBlockHashException(
                    $"The block #{Index} {Hash} has an invalid hash; expected: {expectedHash}."
                );
            }
        }

        /// <summary>
        /// Unsafely creates a <see cref="BlockHeader"/> instance with its
        /// <paramref name="preEvaluationBlockHeader"/> and <paramref name="proof"/> which is
        /// probably considered as to be valid.
        /// </summary>
        /// <param name="preEvaluationBlockHeader">The pre-evaluation block header.</param>
        /// <param name="proof">A triple of the state root hash, the block signature, and the block
        /// hash which is probably considered as to be derived from
        /// the <paramref name="preEvaluationBlockHeader"/> and the state root hash.</param>
        /// <exception cref="InvalidBlockSignatureException">Thrown if a <paramref name="proof"/>'s
        /// signature is invalid.</exception>
        /// <remarks>This does not verify if a <paramref name="proof"/>'s hash is derived from
        /// the <paramref name="preEvaluationBlockHeader"/> and the state root hash.</remarks>
        private BlockHeader(
            PreEvaluationBlockHeader preEvaluationBlockHeader,
            (
                HashDigest<SHA256> StateRootHash,
                ImmutableArray<byte>? Signature,
                BlockHash Hash
            ) proof
        )
        {
            if (!preEvaluationBlockHeader.VerifySignature(proof.Signature, proof.StateRootHash))
            {
                long idx = preEvaluationBlockHeader.Index;
                string msg = preEvaluationBlockHeader.ProtocolVersion >= 2
                    ? $"The block #{idx} #{proof.Hash}'s signature is invalid."
                    : $"The block #{idx} #{proof.Hash} cannot be signed as its protocol version " +
                        $"is less than 2: {preEvaluationBlockHeader.ProtocolVersion}.";
                throw new InvalidBlockSignatureException(
                    preEvaluationBlockHeader.PublicKey,
                    proof.Signature,
                    msg
                );
            }

            _preEvaluationBlockHeader = preEvaluationBlockHeader;
            StateRootHash = proof.StateRootHash;
            Signature = proof.Signature;
            Hash = proof.Hash;
        }

        /// <inheritdoc cref="IBlockMetadata.ProtocolVersion"/>
        public int ProtocolVersion => _preEvaluationBlockHeader.ProtocolVersion;

        /// <inheritdoc cref="IPreEvaluationBlockHeader.HashAlgorithm"/>
        public HashAlgorithmType HashAlgorithm => _preEvaluationBlockHeader.HashAlgorithm;

        /// <inheritdoc cref="IBlockMetadata.Index"/>
        public long Index => _preEvaluationBlockHeader.Index;

        /// <inheritdoc cref="IBlockMetadata.Timestamp"/>
        public DateTimeOffset Timestamp => _preEvaluationBlockHeader.Timestamp;

        /// <inheritdoc cref="IPreEvaluationBlockHeader.Nonce"/>
        public Nonce Nonce => _preEvaluationBlockHeader.Nonce;

        /// <inheritdoc cref="IBlockMetadata.Miner"/>
        public Address Miner => _preEvaluationBlockHeader.Miner;

        /// <inheritdoc cref="IBlockMetadata.PublicKey"/>
        public PublicKey? PublicKey => _preEvaluationBlockHeader.PublicKey;

        /// <inheritdoc cref="IBlockMetadata.Difficulty"/>
        public long Difficulty => _preEvaluationBlockHeader.Difficulty;

        /// <inheritdoc cref="IBlockMetadata.TotalDifficulty"/>
        public BigInteger TotalDifficulty => _preEvaluationBlockHeader.TotalDifficulty;

        /// <inheritdoc cref="IBlockMetadata.PreviousHash"/>
        public BlockHash? PreviousHash => _preEvaluationBlockHeader.PreviousHash;

        /// <inheritdoc cref="IBlockMetadata.TxHash"/>
        public HashDigest<SHA256>? TxHash => _preEvaluationBlockHeader.TxHash;

        /// <inheritdoc cref="IBlockHeader.Signature"/>
        public ImmutableArray<byte>? Signature { get; }

        /// <inheritdoc cref="IBlockExcerpt.Hash"/>
        public BlockHash Hash { get; }

        /// <inheritdoc cref="IPreEvaluationBlockHeader.PreEvaluationHash"/>
        public ImmutableArray<byte> PreEvaluationHash =>
            _preEvaluationBlockHeader.PreEvaluationHash;

        /// <inheritdoc cref="IBlockHeader.StateRootHash"/>
        public HashDigest<SHA256> StateRootHash { get; }

        /// <inheritdoc cref="object.ToString()"/>
        public override string ToString() =>
            $"#{Index} {Hash}";
    }
}
